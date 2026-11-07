export async function act(page, step) {
  if (step.action === "goto") {
    if (!step.value) {
      throw new Error("goto needs a url.");
    }
    await page.goto(step.value, { waitUntil: "domcontentloaded", timeout: 30000 });
    return `opened ${step.value}`;
  }

  if (step.action === "press") {
    const key = step.value || "Enter";
    await page.keyboard.press(key);
    return `pressed ${key}`;
  }

  if (step.action === "assert") {
    const needle = step.text || step.value;
    if (!needle) {
      throw new Error("assert needs text.");
    }
    const body = await page.locator("body").innerText();
    if (!body.toLowerCase().includes(needle.toLowerCase())) {
      throw new Error(`Page text does not include "${needle}".`);
    }
    return `page includes "${needle}"`;
  }

  if (!step.name) {
    throw new Error(`${step.action} needs a name from the snapshot.`);
  }

  const role = step.role || "textbox";
  const locator = page.getByRole(role, { name: step.name });
  const count = await locator.count();
  if (count === 0) {
    throw new Error(`No ${role} named "${step.name}".`);
  }
  if (count > 1) {
    throw new Error(`${count} ${role} elements match "${step.name}". Use a more specific name.`);
  }

  if (step.action === "click") {
    await locator.click({ timeout: 8000 });
    return `clicked ${role} "${step.name}"`;
  }

  if (step.action === "fill") {
    await locator.fill(step.value, { timeout: 8000 });
    return `filled ${role} "${step.name}"`;
  }

  if (step.action === "select") {
    await locator.selectOption({ label: step.value });
    return `selected "${step.value}" in ${role} "${step.name}"`;
  }

  throw new Error(`Cannot run action "${step.action}" here.`);
}
