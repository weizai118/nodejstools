// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.Text;

namespace TestUtilities.Mocks
{
    public class MockTrackingPoint : ITrackingPoint
    {
        private readonly int _position;
        private readonly MockTextSnapshot _snapshot;
        private readonly PointTrackingMode _mode;
        private readonly TrackingFidelityMode _fidelity;

        public MockTrackingPoint(MockTextSnapshot snapshot, int position, PointTrackingMode mode = PointTrackingMode.Positive, TrackingFidelityMode fidelity = TrackingFidelityMode.Forward)
        {
            _position = position;
            _snapshot = snapshot;
            _mode = mode;
            _fidelity = fidelity;
        }

        private SnapshotPoint GetPoint(ITextVersion version)
        {
            if (version.TextBuffer != _snapshot.TextBuffer)
            {
                Debug.Fail("Mismatched buffers");
                throw new ArgumentException("Mismatched text buffers");
            }

            var newPos = _position;
            var current = _snapshot.Version;
            var target = version;
            var toSnapshot = ((MockTextVersion)target)._snapshot;
            if (current.VersionNumber > target.VersionNumber)
            {
                // Apply the changes in reverse
                var changesStack = new Stack<INormalizedTextChangeCollection>();

                for (var v = target; v.VersionNumber < current.VersionNumber; v = v.Next)
                {
                    changesStack.Push(v.Changes);
                }


                while (changesStack.Count > 0)
                {
                    foreach (var change in changesStack.Pop())
                    {
                        if (change.Delta > 0 && change.NewPosition <= newPos && change.NewPosition - change.Delta > newPos)
                        {
                            // point was deleted
                            newPos = change.NewPosition;
                        }
                        else if (change.NewPosition == newPos)
                        {
                            if (_mode == PointTrackingMode.Positive)
                            {
                                newPos -= change.Delta;
                            }
                        }
                        else if (change.NewPosition < newPos)
                        {
                            newPos -= change.Delta;
                        }
                    }
                }
            }
            else if (current.VersionNumber < target.VersionNumber)
            {
                // Apply the changes normally
                for (var v = current; v.VersionNumber < target.VersionNumber; v = v.Next)
                {
                    foreach (var change in v.Changes)
                    {
                        if (change.Delta < 0 && change.OldPosition <= newPos && change.OldPosition - change.Delta > newPos)
                        {
                            // point was deleted
                            newPos = change.OldPosition;
                        }
                        else if (change.OldPosition == newPos)
                        {
                            if (_mode == PointTrackingMode.Positive)
                            {
                                newPos += change.Delta;
                            }
                        }
                        else if (change.OldPosition < newPos)
                        {
                            newPos += change.Delta;
                        }
                    }
                }
            }

            Debug.Assert(newPos >= 0, string.Format("new point '{0}' should be zero or greater", newPos));
            Debug.Assert(newPos <= toSnapshot.Length, string.Format("new point '{0}' should be {1} or less", newPos, toSnapshot.Length));
            return new SnapshotPoint(toSnapshot, newPos);
        }

        public SnapshotPoint GetPoint(ITextSnapshot snapshot)
        {
            return GetPoint(snapshot.Version);
        }

        public char GetCharacter(ITextSnapshot snapshot)
        {
            return GetPoint(snapshot.Version).GetChar();
        }

        public int GetPosition(ITextVersion version)
        {
            return GetPoint(version).Position;
        }

        public int GetPosition(ITextSnapshot snapshot)
        {
            return GetPoint(snapshot).Position;
        }

        public ITextBuffer TextBuffer
        {
            get { return _snapshot.TextBuffer; }
        }

        public TrackingFidelityMode TrackingFidelity
        {
            get { return _fidelity; }
        }

        public PointTrackingMode TrackingMode
        {
            get { return _mode; }
        }
    }
}

