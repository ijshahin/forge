﻿// The MIT License (MIT)
//
// Copyright (c) 2013 Jacob Dufault
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using Forge.Utilities;
using System;
using System.Threading;

namespace Forge.Entities.Implementation.Runtime {
    internal interface DataContainer {
    }

    internal class NonVersionedDataContainer : DataContainer {
        /// <summary>
        /// Used by the Entity to determine if the data inside of this container has already been
        /// modified.
        /// </summary>
        public AtomicActivation MotificationActivation;

        public Data.NonVersioned Data;

        public NonVersionedDataContainer(Data.NonVersioned data) {
            Data = data;
            MotificationActivation = new AtomicActivation();
        }
    }

    /// <summary>
    /// Contains a set of three IData instances and allows swapping between those instances such
    /// that one of them is the previous state, one of them is the current state, and one of them is
    /// a modifiable state.
    /// </summary>
    internal class VersionedDataContainer : DataContainer {
        /// <summary>
        /// All stored immutable data items.
        /// </summary>
        public Data.Versioned[] Items;

        /// <summary>
        /// The index of the previous item.
        /// </summary>
        private int _previousIndex;

        /// <summary>
        /// Used by the Entity to determine if the data inside of this container has already been
        /// modified.
        /// </summary>
        public AtomicActivation MotificationActivation;

        /// <summary>
        /// Initializes a new instance of the ImmutableContainer class.
        /// </summary>
        public VersionedDataContainer(Data.Versioned previous, Data.Versioned current, Data.Versioned modified) {
            Items = new Data.Versioned[] { previous, current, modified };
            MotificationActivation = new AtomicActivation();
        }

        /// <summary>
        /// Return the data instance that contains the current values.
        /// </summary>
        public Data.Versioned Current {
            get {
                return Items[(_previousIndex + 1) % Items.Length];
            }
        }

        /// <summary>
        /// Return the data instance that can be modified.
        /// </summary>
        public Data.Versioned Modifying {
            get {
                return Items[(_previousIndex + 2) % Items.Length];
            }
        }

        /// <summary>
        /// Return the data instance that contains the previous values.
        /// </summary>
        public Data.Versioned Previous {
            get {
                return Items[(_previousIndex + 0) % Items.Length];
            }
        }

        /// <summary>
        /// Updates Modifying/Current/Previous so that they point to the next element
        /// </summary>
        public void Increment() {
            // If the object we modified supports multiple modifications, then we need to give it a
            // chance to resolve those modifications so that it is in a consistent state
            if (Modifying is Data.ConcurrentVersioned) {
                ((Data.ConcurrentVersioned)Modifying).ResolveConcurrentModifications();
            }

            // this code is tricky because we change what data.{Previous,Current,Modifying} refer to
            // when we increment _swappableIndex below

            // when we increment _swappableIndex: modifying becomes current current becomes previous
            // previous becomes modifying

            // current refers to the current _swappableIndex current_modifying : data.Modifying
            // current_current : data.Current current_previous : data.Previous

            // next refers to when _swappableIndex += 1 next_modifying : data.Previous next_current
            // : data.Modifying next_previous : data.Current

            // we want next_modifying to become a copy of current_modifying
            Previous.CopyFrom(Modifying);
            // we want next_current to become a copy of current_modifying
            //Modifying.CopyFrom(data.Modifying);
            // we want next_previous to become a copy of current_current
            //Current.CopyFrom(data.Current);

            // update Modifying/Current/Previous references
            ++_previousIndex;
        }
    }
}