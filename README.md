# SomeAutomation

Two browser checks live in this repo. One fills a form without a fixed selector. The other turns a plain-English sentence into a pass or fail in Edge.

## Resilient form check

The C# project is in `resilient-form`. It types a first name, chooses a gender, and chooses a state. The page URL comes from `resilient-form/fields.json`, and you can replace it with `--url`.

You need the .NET 8 SDK and Microsoft Edge.

From `resilient-form`:

```
dotnet test
dotnet run --project src/ResilientForm -- --url https://app.cloudqa.io/home/AutomationPracticeForm
```

Add `--headless` to hide the window. Edit `fields.json` to change the typed name, the gender option, or the state. A passing run prints `PASS` and exits 0.

The finder reads the visible caption next to each control. Id, name, class, and placeholder cannot select a field on their own. When two visible controls share a label, the earlier one is used. Open shadow roots are scanned. Iframes are searched only when the top document has no match. A renamed label, a closed shadow root, or a custom widget that is not a text box, radio, or `select` will miss. More detail is in `resilient-form/APPROACH.md`.

## Plain-English cases

The Node project is in `plain-english-tests`. You give it a sentence and a page. A free OpenRouter model looks at the ARIA snapshot and chooses the next click, fill, or text check. Nothing in the runner is written for one site.

You need Node.js 20 or newer, Microsoft Edge, and an OpenRouter key.

From `plain-english-tests`:

```
npm install
```

Copy `.env.example` to `.env` and set `OPENROUTER_API_KEY`. Leave `OPENROUTER_MODEL` unset to use `openrouter/free`. To pin a model, set an id that ends in `:free`.

```
node src/cli.js --url examples/dashboard.html --case "Enter ada@example.com in the Email field, click Continue, and verify the page shows Dashboard."
```

```
node src/cli.js --url https://phptravels.com/demo --case "Log in as a customer and verify the dashboard is shown"
```

`--headed` shows the window. `--max-steps` caps the actions. The default is 10. `PASS` exits 0. `FAIL` exits 1.

The model only uses roles and names from the current page, and it only uses a password or email that the sentence or the page already shows. A free model can answer in prose, hit a rate limit, or stop on a captcha. The PHPTravels demo asks for a lead form before it shows a customer login, so a sentence that only says to log in can fail for that reason.
