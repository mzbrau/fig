window.handleScroll = (controls) => {
    if (controls) {
        const controlsTop = controls.getBoundingClientRect().top;

        if (controlsTop <= 0) {
            controls.classList.add('floating-controls');
        } else {
            controls.classList.remove('floating-controls');
        }
    }
};
