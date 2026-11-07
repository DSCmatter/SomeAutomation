export async function observe(page) {
  const url = page.url();
  const title = await page.title();
  let tree = "";
  try {
    tree = await page.locator("body").ariaSnapshot();
  } catch {
    tree = await page.locator("body").innerText();
  }

  if (tree.length > 12000) {
    tree = `${tree.slice(0, 12000)}\n...[truncated]`;
  }

  return { url, title, tree };
}
