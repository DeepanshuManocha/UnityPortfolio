using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class WebGLVideoRawImage : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public RawImage rawImage;
    public RenderTexture renderTexture;
    public string videoFileName = "yourvideo.mp4";

    void Start()
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

    void OnPrepared(VideoPlayer vp)
    {
        vp.Play();
    }
}