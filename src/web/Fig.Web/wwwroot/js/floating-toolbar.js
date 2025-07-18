let scrollHandler = null;
let dotNetRef = null;
let toolbarElement = null;
let isInitialized = false;
let resizeHandler = null;

export function initialize(toolbarRef, dotNetReference) {
    if (isInitialized) {
        cleanup();
    }
    
    try {
        dotNetRef = dotNetReference;
        toolbarElement = toolbarRef;
        
        // Throttled scroll handler for better performance
        let throttleTimer = null;
        scrollHandler = function() {
            if (throttleTimer) {
                clearTimeout(throttleTimer);
            }
            
            throttleTimer = setTimeout(() => {
                if (dotNetRef && toolbarElement) {
                    try {
                        const scrollY = window.pageYOffset || document.documentElement.scrollTop;
                        dotNetRef.invokeMethodAsync('OnScroll', scrollY);
                    } catch (error) {
                        console.warn('Error invoking OnScroll:', error);
                    }
                }
            }, 16); // ~60fps
        };
        
        // Handle window resize to recalculate positions
        resizeHandler = function() {
            if (scrollHandler) {
                scrollHandler();
            }
        };
        
        window.addEventListener('scroll', scrollHandler, { passive: true });
        window.addEventListener('resize', resizeHandler, { passive: true });
        
        isInitialized = true;
    } catch (error) {
        console.error('Error initializing floating toolbar:', error);
    }
}

export function cleanup() {
    try {
        // Clear any pending throttle timer
        if (typeof throttleTimer !== 'undefined' && throttleTimer) {  
            clearTimeout(throttleTimer);  
        }
        
        if (scrollHandler) {
            window.removeEventListener('scroll', scrollHandler);
            scrollHandler = null;
        }
        
        if (resizeHandler) {
            window.removeEventListener('resize', resizeHandler);
            resizeHandler = null;
        }
        
        dotNetRef = null;
        toolbarElement = null;
        isInitialized = false;
    } catch (error) {
        console.warn('Error during cleanup:', error);
    }
}

export function getElementTop(element) {
    try {
        if (!element) return 0;
        
        const rect = element.getBoundingClientRect();
        const scrollTop = window.pageYOffset || document.documentElement.scrollTop;
        return rect.top + scrollTop;
    } catch (error) {
        console.warn('Error getting element top:', error);
        return 0;
    }
}
