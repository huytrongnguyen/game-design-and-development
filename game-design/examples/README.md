# Simulations

Runnable balance simulations for the [Game Design course](../README.md). Only the numeric modules have one. Each folder (`NN-topic/`) is a self-contained C# / .NET 10 project with xUnit tests and an editable data file (JSON or CSV) that holds the design numbers. From the fundamentals folder:

```bash
dotnet test examples/08-ttk
```

Change a number in the data file, run the tests again, and see what moves: that is the point of a design simulation. These are teaching tools, kept small and readable.
