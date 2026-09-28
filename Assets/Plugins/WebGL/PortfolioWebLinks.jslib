mergeInto(LibraryManager.library, {
    // Opens the link on the next pointer release, i.e. inside a real browser user gesture,
    // so pop-up blockers allow new tabs and downloads. mode: 0 = new tab, 1 = same window, 2 = download.
    PortfolioOpenLinkOnRelease: function (urlPtr, fileNamePtr, mode) {
        var url = UTF8ToString(urlPtr);
        var fileName = UTF8ToString(fileNamePtr);

        var open = function () {
            document.removeEventListener('pointerup', open, true);

            if (mode === 2) {
                var anchor = document.createElement('a');
                anchor.href = url;
                anchor.download = fileName;
                document.body.appendChild(anchor);
                anchor.click();
                document.body.removeChild(anchor);
            } else if (mode === 1) {
                window.location.href = url;
            } else {
                window.open(url, '_blank', 'noopener');
            }
        };

        document.addEventListener('pointerup', open, true);
    }
});
