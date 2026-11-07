# Run the form check

The program fills First Name, chooses a Gender option, and chooses a State on a page you pass in. It does not store a selector for those fields.

You need the .NET 8 SDK and Microsoft Edge.

## Run the checks

From this directory, run:

```
dotnet test
```

The test run opens Edge, loads a local page, and checks three things. The field order is reversed. The ids, names, and classes on the visible fields are gone. A hidden copy of the form still has the original ids. A later shadow-dom copy repeats First Name. The run must type into the visible First Name, choose Female, and choose India, and it must leave the decoys empty.

## Run it against a page

`fields.json` holds the page URL and the three fields. To use another page, pass `--url`.

```
dotnet run --project src/ResilientForm -- --url https://app.cloudqa.io/home/AutomationPracticeForm
```

Add `--headless` when you do not want a window. To change the typed name, the gender option, or the state, edit `fields.json`. `kind` is `text`, `radio`, or `select`.

A passing run prints one `PASS` line per field and a final `PASS`. The process exits 0. A miss exits 1. A bad config exits 2. On a field miss, the program writes `artifacts/failure.png`.
