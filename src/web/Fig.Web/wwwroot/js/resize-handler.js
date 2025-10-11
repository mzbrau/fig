// resize-handler.js - Handles panel resize functionality
let dotNetRef = null;
let isResizing = false;

export function initialize(dotNetReference) {
    dotNetRef = dotNetReference;
    
    // Set up global mouse event listeners
    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);
}

export function cleanup() {
    document.removeEventListener('mousemove', handleMouseMove);
    document.removeEventListener('mouseup', handleMouseUp);
}

function handleMouseMove(e) {
    if (!isResizing || !dotNetRef) return;
    
    e.preventDefault();
    dotNetRef.invokeMethodAsync('OnResizeMove', e.clientX);
}

function handleMouseUp(e) {
    if (!isResizing || !dotNetRef) return;
    
    isResizing = false;
    dotNetRef.invokeMethodAsync('OnResizeEnd');
    
    // Remove dragging class from body
    document.body.classList.remove('resizing');
    
    // Re-enable text selection
    document.body.style.userSelect = '';
    document.body.style.webkitUserSelect = '';
    document.body.style.cursor = '';
}

// Called from C# when resize starts
window.startResize = function(newNetReference) {
    isResizing = true;
    dotNetRef = newNetReference;
    
    // Add dragging class to disable transitions
    document.body.classList.add('resizing');
    
    // Disable text selection during drag
    document.body.style.userSelect = 'none';
    document.body.style.webkitUserSelect = 'none';
    document.body.style.cursor = 'col-resize';
};

// Helper function to get viewport width
window.getViewportWidth = function() {
    return window.innerWidth;
};
