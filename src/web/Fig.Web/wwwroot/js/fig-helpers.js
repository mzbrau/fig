// fig-helpers.js - General helper functions used by Blazor JS interop

window.saveAsFile = function(filename, bytesBase64) {
    var link = document.createElement('a');
    link.download = filename;
    link.href = "data:application/octet-stream;base64," + bytesBase64;
    document.body.appendChild(link); // Needed for Firefox
    link.click();
    document.body.removeChild(link);
};

window.scrollIntoView = function(elementId) {
    var elem = document.getElementById(elementId);
    if (!elem) return false;

    // Instant center alignment (no smooth delay)
    elem.scrollIntoView({
        behavior: "auto",
        block: "center",
        inline: "nearest"
    });

    // Keep existing behavior where scroll operation also applies highlight
    if (typeof window.highlightSetting === 'function') {
        window.highlightSetting(elementId);
    }

    return true;
};

window.__highlightControllers = window.__highlightControllers || {};
window.highlightSetting = function(elementId) {
    var existing = window.__highlightControllers[elementId];
    if (existing) {
        clearInterval(existing.keepAliveInterval);
        clearTimeout(existing.removeTimeout);
    }

    function restartHighlightAnimation() {
        var el = document.getElementById(elementId);
        if (!el) return;

        // Restart the animation from the beginning.
        el.classList.remove('glow-highlight');
        void el.offsetWidth;
        el.classList.add('glow-highlight');
    }

    function keepHighlightApplied() {
        var el = document.getElementById(elementId);
        if (!el) return;
        if (!el.classList.contains('glow-highlight')) {
            el.classList.add('glow-highlight');
        }
    }

    restartHighlightAnimation();

    // Keep re-applying during the active window so re-renders don't drop the highlight.
    var keepAliveInterval = setInterval(keepHighlightApplied, 120);

    var removeTimeout = setTimeout(function() {
        clearInterval(keepAliveInterval);
        var el = document.getElementById(elementId);
        if (el) {
            el.classList.remove('glow-highlight');
        }
        delete window.__highlightControllers[elementId];
    }, 2000); // 1s * 2 cycles

    window.__highlightControllers[elementId] = {
        keepAliveInterval: keepAliveInterval,
        removeTimeout: removeTimeout
    };
};

window.downloadCsvFile = function(base64, filename) {
    const link = document.createElement('a');
    link.href = 'data:text/csv;charset=utf-8;base64,' + base64;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};

window.openHtmlInNewTab = function(html) {
    const blob = new Blob([html], { type: 'text/html' });
    const url = URL.createObjectURL(blob);
    const opened = window.open(url, '_blank');
    if (!opened) {
        URL.revokeObjectURL(url);
        return false;
    }
    // Revoke after the new tab has had time to load the blob URL.
    setTimeout(function () { URL.revokeObjectURL(url); }, 60000);
    return true;
};

window.clickElementById = function(elementId) {
    const element = document.getElementById(elementId);
    if (element) {
        element.click();
    } else {
        console.error("clickElementById: Element with ID '" + elementId + "' not found.");
    }
};

// Check if an element's content overflows its visible area.
// Returns true if overflowing, false if not, or null if the element has not been
// laid out yet (clientHeight === 0). A double requestAnimationFrame is used so that
// the browser has finished both style recalculation and layout before measuring.
window.isElementOverflowing = function(element) {
    return new Promise((resolve) => {
        if (!element) {
            resolve(null);
            return;
        }
        requestAnimationFrame(() => {
            requestAnimationFrame(() => {
                if (element.clientHeight === 0) {
                    resolve(null);
                } else {
                    resolve(element.scrollHeight > element.clientHeight + 1);
                }
            });
        });
    });
};

// Function to reset the value of a file input element by its ID
window.resetFileInputValueById = function(elementId) {
    const element = document.getElementById(elementId);
    if (element) {
        // Setting value to null clears the selected file for input type=file
        element.value = null;
    } else {
        console.warn("resetFileInputValueById: Element with ID '" + elementId + "' not found for reset.");
    }
};

// Function to scroll a textarea to the bottom
window.scrollTextAreaToBottom = function(elementId) {
    const element = document.getElementById(elementId);
    if (element) {
        element.scrollTop = element.scrollHeight;
    }
};

// Settings page specific double-shift detection
window.setupSettingsDoubleShiftDetection = function(dotNetObjectReference, timeoutMs) {
    let lastShiftPressTime = 0;
    let shiftPressed = false;
    let dialogOpen = false;

    function handleKeyDown(event) {
        // Don't process shift detection if a dialog is open
        if (dialogOpen) return;

        // Only process shift keys when no modifiers are pressed and target is not an input field
        if (event.key === 'Shift' && !event.ctrlKey && !event.altKey && !event.metaKey) {
            // Don't trigger if focused on an input, textarea, or contenteditable element
            const activeElement = document.activeElement;
            if (activeElement && (
                activeElement.tagName === 'INPUT' ||
                activeElement.tagName === 'TEXTAREA' ||
                activeElement.contentEditable === 'true'
            )) {
                return;
            }

            const now = Date.now();

            if (!shiftPressed) {
                // First shift press
                shiftPressed = true;
                lastShiftPressTime = now;

                // Set timeout to reset shift state if second press doesn't come
                setTimeout(() => {
                    if (shiftPressed && (Date.now() - lastShiftPressTime) >= timeoutMs) {
                        shiftPressed = false;
                    }
                }, timeoutMs);
            } else {
                // Second shift press - check if within time window
                const timeSinceLastPress = now - lastShiftPressTime;
                if (timeSinceLastPress <= timeoutMs) {
                    // Double-shift detected!
                    shiftPressed = false;
                    dialogOpen = true; // Set flag to prevent additional triggers
                    dotNetObjectReference.invokeMethodAsync('OnDoubleShiftDetected').then(() => {
                        // Reset dialog open flag after a short delay
                        setTimeout(() => {
                            dialogOpen = false;
                        }, 1000);
                    });
                } else {
                    // Too much time passed, treat as new first press
                    shiftPressed = true;
                    lastShiftPressTime = now;
                }
            }
        }
    }

    // Listen for dialog close events to reset our flag
    function handleDialogClose(event) {
        // Only reset if clicking outside a dialog or on a dialog close button
        const target = event.target;
        const isDialogContent = target.closest('.rz-dialog, .modal, [role="dialog"]');
        const isCloseButton = target.closest('.rz-dialog-close, .modal-close, [data-dismiss="modal"]') ||
                            target.classList.contains('rz-dialog-close') ||
                            target.classList.contains('modal-close');

        // Reset flags if clicking outside dialog or on a close button
        if (!isDialogContent || isCloseButton) {
            dialogOpen = false;
            shiftPressed = false;
        }
    }

    // Add event listeners
    document.addEventListener('keydown', handleKeyDown);
    document.addEventListener('click', handleDialogClose, true); // Use capture phase for better detection

    // Return an object with a cleanup method
    return {
        cleanup: function() {
            document.removeEventListener('keydown', handleKeyDown);
            document.removeEventListener('click', handleDialogClose, true); // Match capture:true used on add
        }
    };
};

// Cleanup function for double-shift detection
window.cleanupSettingsDoubleShiftDetection = function(detectionObject) {
    if (detectionObject && detectionObject.cleanup) {
        detectionObject.cleanup();
    }
};
