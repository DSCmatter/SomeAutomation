# How the fields are found

The practice form is built so that a fixed id, name, class, or XPath breaks when the markup moves. The stable part is the text a person reads: "First Name", "Gender", "Female", "State", "India".

The program reads `fields.json` as the intent. Each entry is a label, a kind, and a value. The URL comes from that file, and `--url` replaces it. The rest of the code never names `fname`, `gender`, or `state`.

On each field, a scan walks the top document, then open shadow roots. For every visible text box, radio, and dropdown it collects nearby captions. A caption is an associated label, an aria name, or a short sibling that does not itself contain another control. Id, name, and placeholder are stored separately and capped at a score of 40. The accept line is 80, so those attributes cannot pick a field on their own.

Scoring compares the words. An exact caption scores 100. One extra word still clears 80, so "First Name required" can match "First Name". "Last Name" against "First Name", and "State of mind" against "State", stay under 80. When two visible controls tie, the earlier one in the page wins. That is why the real form beats the later copies on the practice page.

Radios need two hits. The group caption must match the label, and the text beside the button, or its value, must match the option. Dropdowns match the label the same way as a text box, then pick the option by its visible text.

Iframes are a fallback. The scan enters them only when the top document has no match. A closed shadow root is not visible to the scan.

## Limits

The visible words still have to be the words in the config. Renaming "First Name" to "Given name" means editing `fields.json`. That file is the case, not a locator.

A caption buried two controls away is ignored, so a label that is only a distant heading will miss. A radio whose option text is only a background image will miss. Closed shadow DOM will miss. If two different visible fields share the exact same label, the upper one is used, which is wrong when the one you wanted is lower on the page.

The scan does not understand a custom widget that is a pile of divs with no text box, radio, or select. The practice page's State control is a real `select`, and that is what this handles.
