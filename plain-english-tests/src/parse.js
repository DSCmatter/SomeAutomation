const actions = new Set(["goto", "click", "fill", "select", "press", "assert", "pass", "fail"]);

export function parseAction(text) {
  if (!text || typeof text !== "string") {
    throw new Error("Model reply was empty.");
  }

  const start = text.indexOf("{");
  const end = text.lastIndexOf("}");
  if (start < 0 || end <= start) {
    throw new Error("Model reply did not contain a JSON object.");
  }

  let raw;
  try {
    raw = JSON.parse(text.slice(start, end + 1));
  } catch {
    throw new Error("Model reply JSON did not parse.");
  }

  const action = String(raw.action || "").toLowerCase();
  if (!actions.has(action)) {
    throw new Error(`Model reply used an unknown action "${action}".`);
  }

  return {
    action,
    role: raw.role ? String(raw.role) : "",
    name: raw.name ? String(raw.name) : "",
    value: raw.value == null ? "" : String(raw.value),
    text: raw.text == null ? "" : String(raw.text),
    note: raw.note ? String(raw.note) : "",
  };
}
