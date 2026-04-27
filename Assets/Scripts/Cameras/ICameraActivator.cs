using Unity.Cinemachine;

public interface ICameraActivator
{
    void Activate(CinemachineCamera camera);
    void Deactivate(CinemachineCamera camera);
}