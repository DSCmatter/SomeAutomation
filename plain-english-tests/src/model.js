import { parseAction } from "./parse.js";

const systemPrompt = `You carry out one browser test written in plain English.
You see the page URL, the title, and an ARIA snapshot. Nothing in this prompt is specific to one website.
The whole reply is one JSON object. No markdown and no prose around it.
Shapes:
{"action":"goto","value":"https://example.com"}
{"action":"click","role":"button","name":"Sign in"}
{"action":"fill","role":"textbox","name":"Email","value":"ada@example.com"}
{"action":"select","role":"combobox","name":"Country","value":"India"}
{"action":"press","value":"Enter"}
{"action":"assert","text":"Dashboard"}
{"action":"pass","note":"why the case passed"}
{"action":"fail","note":"why the case cannot pass"}
Rules:
- role and name must come from the snapshot.
- Use a credential only when the test case or the page states it.
- When the case says to verify something, assert that text, then pass.
- When the page has no way to finish the case, fail and say what is missing.
- Do not invent a selector, a private URL, or a password.`;

export async function requestAction({ apiKey, model, testCase, history, observation, problem }) {
  const transcript = history.length === 0 ? "No actions yet." : history.join("\n");
  const problemLine = problem ? `Last action failed: ${problem}` : "Last action succeeded.";
  const user = [
    `Test case: ${testCase}`,
    `Current URL: ${observation.url}`,
    `Title: ${observation.title}`,
    problemLine,
    "Actions so far:",
    transcript,
    "ARIA snapshot:",
    observation.tree,
  ].join("\n");

  const body = await postChat({ apiKey, model, user });
  const content = messageText(body.choices?.[0]?.message?.content);
  if (!content) {
    throw new Error("OpenRouter returned an empty message.");
  }

  try {
    return parseAction(content);
  } catch (error) {
    const snippet = content.replace(/\s+/g, " ").slice(0, 180);
    throw new Error(`${error.message} Reply started: ${snippet}`);
  }
}

function messageText(content) {
  if (typeof content === "string") {
    return content;
  }
  if (Array.isArray(content)) {
    return content.map((part) => part?.text || "").join("\n");
  }
  return "";
}

async function postChat({ apiKey, model, user }) {
  let lastError = "OpenRouter request failed.";
  for (let attempt = 1; attempt <= 3; attempt += 1) {
    const response = await fetch("https://openrouter.ai/api/v1/chat/completions", {
      method: "POST",
      headers: {
        Authorization: `Bearer ${apiKey}`,
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        model,
        temperature: 0,
        messages: [
          { role: "system", content: systemPrompt },
          { role: "user", content: user },
        ],
      }),
    });

    if (response.status === 429 || response.status >= 500) {
      lastError = `OpenRouter returned ${response.status}.`;
      await delay(attempt * 1500);
      continue;
    }

    const payload = await response.json();
    if (!response.ok) {
      const message = payload?.error?.message || `OpenRouter returned ${response.status}.`;
      throw new Error(message);
    }

    return payload;
  }

  throw new Error(lastError);
}

function delay(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}
