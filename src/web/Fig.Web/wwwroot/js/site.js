// Function to programmatically click an element by its ID
function clickElementById(elementId) {
  const element = document.getElementById(elementId);
  if (element) {
    element.click();
  } else {
    // Log an error to the console for easier debugging if the element isn't found
    console.error("clickElementById: Element with ID '" + elementId + "' not found.");
    // Optionally, throw an error to make it more explicitly fail on the .NET side if desired
    throw new Error("clickElementById: Element with ID '" + elementId + "' not found.");
  }
}

// Function to reset the value of a file input element by its ID
function resetFileInputValueById(elementId) {
  const element = document.getElementById(elementId);
  if (element) {
    // Setting value to null clears the selected file for input type=file
    element.value = null;
  } else {
    // Log a warning if the element isn't found; this might be less critical than click failing
    console.warn("resetFileInputValueById: Element with ID '" + elementId + "' not found for reset.");
  }
}
