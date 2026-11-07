import { act } from "./act.js";
import { requestAction } from "./model.js";
import { observe } from "./observe.js";

export async function runCase({ page, apiKey, model, testCase, startUrl, maxSteps }) {
  await page.goto(startUrl, { waitUntil: "domcontentloaded", timeout: 45000 });
  const history = [];
  let problem = "";

  for (let stepNumber = 1; stepNumber <= maxSteps; stepNumber += 1) {
    const observation = await observe(page);
    let step;
    try {
      step = await requestAction({
        apiKey,
        model,
        testCase,
        history,
        observation,
        problem,
      });
    } catch (error) {
      history.push(`${stepNumber}. model reply rejected: ${error.message}`);
      problem = `${error.message} Reply with one JSON object only.`;
      continue;
    }

    if (step.action === "pass") {
      history.push(`${stepNumber}. pass ${step.note}`.trim());
      return finish(true, history, step.note || "The model marked the case passed.");
    }

    if (step.action === "fail") {
      history.push(`${stepNumber}. fail ${step.note}`.trim());
      return finish(false, history, step.note || "The model marked the case failed.");
    }

    try {
      const detail = await act(page, step);
      history.push(`${stepNumber}. ${detail}`);
      problem = "";
    } catch (error) {
      history.push(`${stepNumber}. ${step.action} failed: ${error.message}`);
      problem = error.message;
    }
  }

  return finish(false, history, `Stopped after ${maxSteps} actions.`);
}

function finish(passed, history, note) {
  return { passed, history, note };
}

export function formatReport(result) {
  const lines = [result.passed ? "PASS" : "FAIL", ...result.history];
  if (result.note) {
    lines.push(result.note);
  }
  return lines.join("\n");
}
