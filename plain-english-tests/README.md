# Run a plain-English case

The tool reads one sentence, opens a page in Edge, and asks a free OpenRouter model what to do next. The model sees an ARIA snapshot of whatever page is open. The tool then clicks, fills, or checks text, and prints `PASS` or `FAIL`.

You need Node.js 20 or newer, Microsoft Edge, and an OpenRouter key. The default model is `openrouter/free`, which stays on free models.

## Set the key

Copy `.env.example` to `.env` in this directory. Set `OPENROUTER_API_KEY`. To pin one free model, set `OPENROUTER_MODEL` to an id that ends in `:free`. Leave it unset to use `openrouter/free`.

Install the packages:

```
npm install
```

## Run a case

Pass the sentence and the page. The sentence can name any site. The tool does not contain steps for a particular site.

```
node src/cli.js --url https://phptravels.com/demo --case "Log in as a customer and verify the dashboard is shown"
```

`--headed` shows the window. `--max-steps` caps the actions. The default is 10. If you omit `--url`, the tool uses the first http(s) URL in the sentence.

A local page is included so you can see a pass without a third-party site:

```
node src/cli.js --url examples/dashboard.html --case "Enter ada@example.com in the Email field, click Continue, and verify the page shows Dashboard."
```

`PASS` exits 0. `FAIL` exits 1. A missing key or a missing URL exits 2.

Parser checks, with no network and no browser:

```
npm test
```
