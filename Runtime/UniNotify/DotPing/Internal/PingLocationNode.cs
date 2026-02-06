namespace UniCore.Notify.DotPing.Internal
{
    internal struct PingLocationNode
    {
        public string id;
        public string parentId;
        public int value;
        public bool hasChild;
        public bool isActive => value > 0 || hasChild;
    }
}