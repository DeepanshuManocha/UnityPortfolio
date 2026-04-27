using Unity.Cinemachine;

public sealed class GameObjectActivator : ICameraActivator
{
    public void Activate(CinemachineCamera camera)
    {
        if (camera != null) camera.gameObject.SetActive(true);
    }

    public void Deactivate(CinemachineCamera camera)
    {
        if (camera != null) camera.gameObject.SetActive(false);
    }
}