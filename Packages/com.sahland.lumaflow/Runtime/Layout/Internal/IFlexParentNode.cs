namespace LumaFlow {

    /// <summary>
    /// Marks layout nodes that allocate their immediate children with flex rules.
    /// </summary>
    internal interface IFlexParentNode {
        /// <summary>
        /// Reapplies the layout-owned spacing for direct native children after a
        /// transparent descendant changes its mounted subtree.
        /// </summary>
        void RefreshChildSpacing();
    }

}
