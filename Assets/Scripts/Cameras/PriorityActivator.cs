using Unity.Cinemachine;

public sealed class PriorityActivator : ICameraActivator
{
    private readonly int _activePriority;
    private readonly int _inactivePriority;

    public PriorityActivator(int activePriority = 20, int inactivePriority = 0)
    {
        _activePriority = activePriority;
        _inactivePriority = inactivePriority;
    }

    public void Activate(CinemachineCamera camera)
    {
        if (camera != null) camera.Priority = _activePriority;
    }

    public void Deactivate(CinemachineCamera camera)
    {
        if (camera != null) camera.Priority = _inactivePriority;
    }
}