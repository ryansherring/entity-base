namespace EntityBase
{
    /// <summary>
    /// Available locomotion modes for EntityMotor.
    /// Ground uses gravity + jumping, Swimming uses buoyancy + 3D water movement,
    /// Flying uses free 3D movement with no gravity.
    /// </summary>
    public enum LocomotionMode
    {
        Ground = 0,
        Swimming = 1,
        Flying = 2
    }
}
