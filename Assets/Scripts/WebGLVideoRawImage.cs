using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class WebGLVideoRawImage : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage rawImage;
    [SerializeField] private RenderTexture renderTexture;
    [SerializeField] private string videoFileName = "yourvideo.mp4";

    private void Start()
    {
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = Application.streamingAssetsPath + "/" + videoFileName;

        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = renderTexture;
        rawImage.texture = renderTexture;

        renderTexture.Release();
        renderTexture.Create();

        videoPlayer.audioOutputMode = VideoAudioOutputMode.None;

        videoPlayer.prepareCompleted += OnPrepared;
        videoPlayer.errorReceived += (vp, msg) =>
        {
            Debug.LogError("VideoPlayer error: " + msg + " URL: " + vp.url);
        };

        videoPlayer.Prepare();
    }

    private void OnPrepared(VideoPlayer vp)
    {
        vp.Play();
    }
}