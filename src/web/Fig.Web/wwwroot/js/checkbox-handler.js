// Checkbox handler for shift key detection
let shiftKeyState = false;
let dotNetObjectReference = null;

// Initialize with Blazor component reference
window.initializeShiftKeyTracking = function(dotNetRef) {
    dotNetObjectReference = dotNetRef;
};

// Allow Blazor to detach to prevent calls after component disposal
window.cleanupShiftKeyTracking = function() {
        dotNetObjectReference = null;
        shiftKeyState = false;
};

// Track shift key state globally and notify Blazor
document.addEventListener('keydown', function(event) {
    if (event.key === 'Shift' && !shiftKeyState) {
        shiftKeyState = true;
        if (dotNetObjectReference) {
            dotNetObjectReference.invokeMethodAsync('OnShiftKeyChanged', true).catch(() => {});
        }
    }
});

document.addEventListener('keyup', function(event) {
    if (event.key === 'Shift' && shiftKeyState) {
        shiftKeyState = false;
        if (dotNetObjectReference) {
            dotNetObjectReference.invokeMethodAsync('OnShiftKeyChanged', false);
        }
    }
});