$('html, body').bind('scroll mousedown wheel DOMMouseScroll mousewheel keyup', function(evt) {
    if (evt.type === 'DOMMouseScroll' || evt.type === 'keyup' || evt.type === 'mousewheel') {
        if (evt.originalEvent.detail < 0 || (evt.originalEvent.wheelDelta && evt.originalEvent.wheelDelta > 0)) {
            // scroll up
            autoscroll = false;
        } else if (evt.originalEvent.detail > 0 || (evt.originalEvent.wheelDelta && evt.originalEvent.wheelDelta < 0)) {
            // scroll down
            // re-enables autoscroll if the user scrolls to the bottom of the page
            autoscroll = $(window).scrollTop() + $(window).height() === $(document).height();
        }
    }
});