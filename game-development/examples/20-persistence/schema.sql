-- PostgreSQL equivalent of the in-memory store (module 17). Written for PostgreSQL 16+;
-- not executed by the tests, which use the in-memory store behind the same interface.

CREATE TABLE players (
    id    text   PRIMARY KEY,
    gold  bigint NOT NULL DEFAULT 0 CHECK (gold >= 0)          -- the database refuses a negative balance
);

CREATE TABLE player_items (
    player_id text   NOT NULL REFERENCES players(id),
    item_id   text   NOT NULL,
    count     int    NOT NULL CHECK (count > 0),
    PRIMARY KEY (player_id, item_id)
);

-- One row per operation id: the idempotency key. The PRIMARY KEY is what makes "at most once" true.
CREATE TABLE applied_operations (
    op_id      text        PRIMARY KEY,
    kind       text        NOT NULL,
    applied_at timestamptz NOT NULL DEFAULT now()
);

-- Append-only audit log, written in the same transaction as the change it describes.
CREATE TABLE event_log (
    seq     bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    op_id   text   NOT NULL REFERENCES applied_operations(op_id),
    kind    text   NOT NULL,
    changes jsonb  NOT NULL,
    at      timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE sagas (
    id     text  PRIMARY KEY,
    status text  NOT NULL,
    "order" jsonb NOT NULL
);

CREATE TABLE snapshots (
    player_id text PRIMARY KEY REFERENCES players(id),
    zone text NOT NULL, x int NOT NULL, y int NOT NULL, exp bigint NOT NULL
);

-- Step 2 of the trade, as one transaction. Returns 0 on success, a negative code per failure
-- (the same idea as per-step error codes in a stored procedure), and 1 for "already applied".
CREATE FUNCTION settle_trade(
    p_op_id text, p_escrow text, p_buyer text, p_seller text,
    p_item text, p_qty int, p_price bigint) RETURNS int
LANGUAGE plpgsql AS $$
BEGIN
    -- Idempotency: if this op id was already applied, do nothing.
    INSERT INTO applied_operations(op_id, kind) VALUES (p_op_id, 'trade.settle')
        ON CONFLICT (op_id) DO NOTHING;
    IF NOT FOUND THEN RETURN 1; END IF;

    -- Take items out of escrow; the WHERE clause is the validation.
    UPDATE player_items SET count = count - p_qty
        WHERE player_id = p_escrow AND item_id = p_item AND count >= p_qty;
    IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE = 'P0001', MESSAGE = '-1'; END IF;
    DELETE FROM player_items WHERE player_id = p_escrow AND item_id = p_item AND count = 0;

    -- Buyer pays. The conditional UPDATE is atomic: no read-then-write race.
    UPDATE players SET gold = gold - p_price WHERE id = p_buyer AND gold >= p_price;
    IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE = 'P0001', MESSAGE = '-2'; END IF;

    UPDATE players SET gold = gold + p_price WHERE id = p_seller;
    IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE = 'P0001', MESSAGE = '-3'; END IF;

    INSERT INTO player_items(player_id, item_id, count) VALUES (p_buyer, p_item, p_qty)
        ON CONFLICT (player_id, item_id) DO UPDATE SET count = player_items.count + EXCLUDED.count;

    INSERT INTO event_log(op_id, kind, changes)
        VALUES (p_op_id, 'trade.settle',
                jsonb_build_object('buyer', p_buyer, 'seller', p_seller, 'item', p_item, 'qty', p_qty, 'price', p_price));
    RETURN 0;
EXCEPTION WHEN SQLSTATE 'P0001' THEN
    -- The function body is one transaction: raising undoes the idempotency row and every
    -- update above. The caller sees the code and may run the compensating step.
    RETURN SQLERRM::int;
END $$;
