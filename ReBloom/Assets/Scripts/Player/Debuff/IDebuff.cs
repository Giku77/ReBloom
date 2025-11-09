public interface IDebuff
{
    int ID { get; }
    string Name { get; }
    int Category { get; }
    bool IsActive { get; }
    
    void Apply(PlayerStats target);
    void Remove(PlayerStats target);
    void Tick(float deltaTime);
    bool ShouldRemove();
}