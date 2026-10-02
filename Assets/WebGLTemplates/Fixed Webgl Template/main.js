const canvas = document.querySelector("#unity-canvas");
const loadingEl = document.querySelector("#loading");
const loadingFill = document.querySelector("#loadingFill");
const loadingTip = document.querySelector("#loadingTip");
const fullscreenBtn = document.querySelector("#fullscreen-btn");

let myGameInstance = null;

const buildUrl = "Build";
const config = {
  dataUrl: buildUrl + "/{{{ DATA_FILENAME }}}",
  frameworkUrl: buildUrl + "/{{{ FRAMEWORK_FILENAME }}}",
  codeUrl: buildUrl + "/{{{ CODE_FILENAME }}}",
  streamingAssetsUrl: "StreamingAssets",
  companyName: "{{{ COMPANY_NAME }}}",
  productName: "{{{ PRODUCT_NAME }}}",
  productVersion: "{{{ PRODUCT_VERSION }}}",
  matchWebGLToCanvasSize: true,
};

if (/iPhone|iPad|iPod|Android/i.test(navigator.userAgent)) {
  const meta = document.createElement("meta");
  meta.name = "viewport";
  meta.content =
    "width=device-width, height=device-height, initial-scale=1.0, user-scalable=no, shrink-to-fit=yes";
  document.head.appendChild(meta);
}

function resizeCanvasDisplay() {
  const viewportWidth = window.innerWidth;
  const viewportHeight = window.innerHeight;
  const targetAspect = 16 / 9;

  let displayWidth = viewportWidth;
  let displayHeight = viewportWidth / targetAspect;

  if (displayHeight > viewportHeight) {
    displayHeight = viewportHeight;
    displayWidth = viewportHeight * targetAspect;
  }

  canvas.style.width = `${displayWidth}px`;
  canvas.style.height = `${displayHeight}px`;
}

// Unity reports ~0-0.9 while downloading and the rest while starting up.
function setProgress(progress) {
  loadingFill.style.width = `${Math.round(progress * 100)}%`;
  if (progress >= 0.9) {
    loadingTip.textContent = "Setting up the room…";
  }
}

function revealExperience() {
  requestAnimationFrame(() => {
    canvas.classList.add("loaded");
  });

  setTimeout(() => {
    loadingEl.classList.add("done");
  }, 250);

  setTimeout(() => {
    fullscreenBtn.classList.add("show");
  }, 900);
}

fullscreenBtn.addEventListener("click", () => {
  if (myGameInstance) {
    myGameInstance.SetFullscreen(1);
  }
});

resizeCanvasDisplay();
window.addEventListener("resize", resizeCanvasDisplay);

createUnityInstance(canvas, config, setProgress)
  .then((instance) => {
    myGameInstance = instance;
    resizeCanvasDisplay();
    setProgress(1);

    setTimeout(revealExperience, 250);
  })
  .catch((message) => {
    console.error("Unity failed to load:", message);
    loadingTip.textContent = "Something went wrong loading the room — please refresh.";
    loadingTip.classList.add("error");
  });
