namespace UniCore.Notify.DotPing.Internal
{
    internal struct PingLocationNode
    {
        public string Id;
        public string ParentId;
        public int Value;
        public bool HasChild;
        public bool IsActive => Value > 0 || HasChild;
    }
}