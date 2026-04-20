const canvas = document.querySelector("#unity-canvas");
const loadingOverlay = document.querySelector("#loading-overlay");
const loadingBar = document.querySelector("#loadingBar");
const fullscreenBtn = document.querySelector("#fullscreen-btn");

let myGameInstance = null;

const buildUrl = "Build";
const config = {
  dataUrl: buildUrl + "/FBX BUild.data.unityweb",
  frameworkUrl: buildUrl + "/FBX BUild.framework.js.unityweb",
  codeUrl: buildUrl + "/FBX BUild.wasm.unityweb",
  streamingAssetsUrl: "StreamingAssets",
  companyName: "DefaultCompany",
  productName: "My project",
  productVersion: "0.1.0",
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

function updateLoadingVisual(progress) {
  if (!loadingBar) return;

  if (progress > 0.02) {
    loadingBar.classList.add("active");
  }

  if (progress > 0.85) {
    loadingBar.classList.add("near-done");
  }

  const minScale = 0.92;
  const maxScale = 1.08;
  const scale = minScale + (maxScale - minScale) * progress;

  const minOpacity = 0.35;
  const maxOpacity = 1;
  const opacity = minOpacity + (maxOpacity - minOpacity) * progress;

  loadingBar.style.transform = `scale(${scale})`;
  loadingBar.style.opacity = String(opacity);
}

function revealExperience() {
  requestAnimationFrame(() => {
    canvas.classList.add("loaded");
  });

  setTimeout(() => {
    loadingOverlay.classList.add("hidden");
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

createUnityInstance(canvas, config, (progress) => {
  updateLoadingVisual(progress);
})
  .then((instance) => {
    myGameInstance = instance;
    resizeCanvasDisplay();
    updateLoadingVisual(1);

    setTimeout(() => {
      revealExperience();
    }, 250);
  })
  .catch((message) => {
    console.error("Unity failed to load:", message);

    loadingOverlay.innerHTML = `
      <div style="
        color: white;
        text-align: center;
        font-family: Arial, sans-serif;
        padding: 24px;
        max-width: 600px;
      ">
        <h2 style="margin: 0 0 12px 0;">Failed to load</h2>
        <p style="margin: 0; opacity: 0.85;">${String(message)}</p>
      </div>
    `;
  });
