import assert from "node:assert/strict";
import test from "node:test";
import { parseAction } from "../src/parse.js";

test("reads a JSON action wrapped in extra text", () => {
  const action = parseAction('Sure.\n{"action":"assert","text":"Dashboard"}\n');
  assert.equal(action.action, "assert");
  assert.equal(action.text, "Dashboard");
});

test("rejects an unknown action", () => {
  assert.throws(() => parseAction('{"action":"hack"}'), /unknown action/);
});

test("rejects a reply with no object", () => {
  assert.throws(() => parseAction("I cannot do that."), /JSON object/);
});
