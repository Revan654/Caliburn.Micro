using System.Collections.Generic;

namespace Caliburn.Micro
{
    /// <summary>
    /// The result of a test whether an instance can be closed.
    /// </summary>
    /// <typeparam name="T">The type of the children of the instance.</typeparam>
    public class CloseResult<T> : ICloseResult<T>
    {
        /// <summary>
        /// Creates an instance of the <see cref="CloseResult{T}"/>
        /// </summary>
        /// <param name="CloseCanOccur">Whether of not a close operation should occur.</param>
        /// <param name="Children">The children of the instance that can be closed.</param>
        public CloseResult(bool CloseCanOccur, IEnumerable<T> Children)
        {
            this.CloseCanOccur = CloseCanOccur;
            this.Children = Children;
        }

        /// <summary>
        /// Whether of not a close operation should occur.
        /// </summary>
        public bool CloseCanOccur { get; }

        /// <summary>
        /// The children of the instance that can be closed.
        /// </summary>
        public IEnumerable<T> Children { get; }
    }
}
