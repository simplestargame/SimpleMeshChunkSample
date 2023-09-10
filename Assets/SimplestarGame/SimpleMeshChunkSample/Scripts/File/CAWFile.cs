namespace SimplestarGame
{
    /// <summary>
    /// .caw ファイル定数とか
    /// </summary>
    internal class CAWFile
    {
        /// <summary>
        /// キューブの +X 方向に面するう頂点で作られている三角形リストを意味する
        /// </summary>
        public const int PLUS_X = 0;
        /// <summary>
        /// キューブの +Y 方向に面するう頂点で作られている三角形リストを意味する
        /// </summary>
        public const int PLUS_Y = 1;
        /// <summary>
        /// キューブの +Z 方向に面するう頂点で作られている三角形リストを意味する
        /// </summary>
        public const int PLUS_Z = 2;
        /// <summary>
        /// キューブの -X 方向に面するう頂点で作られている三角形リストを意味する
        /// </summary>
        public const int MINUS_X = 3;
        /// <summary>
        /// キューブの -Y 方向に面するう頂点で作られている三角形リストを意味する
        /// </summary>
        public const int MINUS_Y = 4;
        /// <summary>
        /// キューブの -Z 方向に面するう頂点で作られている三角形リストを意味する
        /// </summary>
        public const int MINUS_Z = 5;
        /// <summary>
        /// キューブの上記いずれの方向にも面していない頂点で作られている三角形リストを意味する
        /// </summary>
        public const int REMAIN = 6;
    }
}
