namespace Course.Sync;

public class PredictionTests
{
    [Fact]
    public void Predict_MovesTheClientImmediatelyWithoutWaitingForTheServer()
    {
        var client = new PredictedClient(50, 50, speed: 2);

        client.Predict(1, 0);

        Assert.Equal(52f, client.X);
        Assert.Equal(1, client.PendingCount);
    }

    [Fact]
    public void Reconcile_WhenPredictionWasRight_CausesNoCorrection()
    {
        var client = new PredictedClient(50, 50, speed: 2);
        var server = new AuthoritativeMover(50, 50, speed: 2);

        client.Reconcile(server.Process(client.Predict(1, 0)));

        Assert.Equal(0f, client.LastCorrection);
        Assert.Equal(0, client.PendingCount);
        Assert.Equal(server.X, client.X);
    }

    [Fact]
    public void Reconcile_WithInputsStillInFlight_ReplaysThemSoThereIsNoVisibleJump()
    {
        var client = new PredictedClient(50, 50, speed: 2);
        var server = new AuthoritativeMover(50, 50, speed: 2);
        var first = client.Predict(1, 0);
        client.Predict(1, 0);
        client.Predict(1, 0);                       // predicted x = 56, all three still unacknowledged

        client.Reconcile(server.Process(first));    // the server has only seen the first input (x = 52)

        Assert.Equal(56f, client.X);                // replayed inputs 2 and 3 on top of the server's 52
        Assert.Equal(0f, client.LastCorrection);
        Assert.Equal(2, client.PendingCount);
    }

    [Fact]
    public void Reconcile_WhenServerDisagrees_PullsTheClientToTheAuthoritativePosition()
    {
        var client = new PredictedClient(50, 50, speed: 2);
        var server = new AuthoritativeMover(50, 50, speed: 1);   // the server applies a slow effect the client did not know about
        var inputs = new[] { client.Predict(1, 0), client.Predict(1, 0), client.Predict(1, 0) };

        client.Reconcile(server.Process(inputs[0]));             // server: 51; client replays 2 inputs at speed 2 -> 55 (was 56)
        Assert.Equal(55f, client.X);
        Assert.Equal(1f, client.LastCorrection);

        client.Reconcile(server.Process(inputs[1]));
        client.Reconcile(server.Process(inputs[2]));             // everything acknowledged: the server's truth wins
        Assert.Equal(53f, client.X);
        Assert.Equal(server.X, client.X);
        Assert.Equal(0, client.PendingCount);
    }

    [Fact]
    public void Step_ClampsAtTheWorldEdgeOnBothSides()
    {
        var client = new PredictedClient(99, 50, speed: 2);
        var server = new AuthoritativeMover(99, 50, speed: 2);

        client.Reconcile(server.Process(client.Predict(1, 0)));

        Assert.Equal(100f, client.X);
        Assert.Equal(100f, server.X);
        Assert.Equal(0f, client.LastCorrection);
    }
}
