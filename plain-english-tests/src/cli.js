import "dotenv/config";
import path from "node:path";
import { pathToFileURL } from "node:url";
import { chromium } from "playwright";
import { formatReport, runCase } from "./agent.js";

const help = `Usage: node src/cli.js --case "plain English case" [--url https://...] [--headed] [--max-steps 10]
Put OPENROUTER_API_KEY in the environment or in a .env file in this directory.
OPENROUTER_MODEL defaults to openrouter/free.`;

const options = parseArgs(process.argv.slice(2));
if (options.help) {
  console.log(help);
  process.exit(0);
}

const apiKey = process.env.OPENROUTER_API_KEY;
if (!apiKey) {
  console.error("Set OPENROUTER_API_KEY in the environment or in .env.");
  process.exit(2);
}

const testCase = options.case;
if (!testCase) {
  console.error(help);
  process.exit(2);
}

const startUrl = resolveStart(options.url || firstUrl(testCase));
if (!startUrl) {
  console.error("Pass --url or include an http(s) URL in the test case.");
  process.exit(2);
}

const model = process.env.OPENROUTER_MODEL || "openrouter/free";
const browser = await chromium.launch({
  channel: process.env.BROWSER_CHANNEL || "msedge",
  headless: !options.headed,
});

try {
  const page = await browser.newPage();
  const result = await runCase({
    page,
    apiKey,
    model,
    testCase,
    startUrl,
    maxSteps: options.maxSteps,
  });
  console.log(formatReport(result));
  console.log(`model: ${model}`);
  process.exitCode = result.passed ? 0 : 1;
} finally {
  await browser.close();
}

function resolveStart(value) {
  if (!value) {
    return "";
  }
  if (/^(https?|file):\/\//i.test(value)) {
    return value;
  }
  return pathToFileURL(path.resolve(value)).href;
}

function firstUrl(text) {
  const match = text.match(/https?:\/\/[^\s)]+/);
  if (!match) {
    return "";
  }
  return match[0].replace(/[.,]$/, "");
}

function parseArgs(argv) {
  const parsed = { help: false, headed: false, case: "", url: "", maxSteps: 10 };
  for (let i = 0; i < argv.length; i += 1) {
    const arg = argv[i];
    if (arg === "--help" || arg === "-h") {
      parsed.help = true;
    } else if (arg === "--headed") {
      parsed.headed = true;
    } else if (arg === "--case") {
      parsed.case = argv[i + 1] || "";
      i += 1;
    } else if (arg === "--url") {
      parsed.url = argv[i + 1] || "";
      i += 1;
    } else if (arg === "--max-steps") {
      parsed.maxSteps = Number(argv[i + 1] || "10");
      i += 1;
    } else {
      throw new Error(`Unknown argument "${arg}".`);
    }
  }

  if (!Number.isInteger(parsed.maxSteps) || parsed.maxSteps < 1) {
    throw new Error("--max-steps needs a positive integer.");
  }

  return parsed;
}
