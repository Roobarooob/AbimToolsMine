namespace AbimToolsMine
{
    public enum FinishingCategory
    {
        Floors,
        Ceilings,
        Walls
    }

    /// <summary>
    /// Пара: из какого параметра помещения ? в какой параметр элемента отделки.
    /// </summary>
    public class RoomParamMapping
    {
        public string SourceParam { get; set; }
        public string TargetParam { get; set; }

        public RoomParamMapping() { }

        public RoomParamMapping(string source, string target)
        {
            SourceParam = source;
            TargetParam = target;
        }
    }
}
