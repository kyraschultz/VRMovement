/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Interaction
{
    /// <summary>
    /// Moves the object it's attached to when an Interactor selects that object.
    /// </summary>
    public class Grabbable : PointableElement, IGrabbable
    {
        /// <summary>
        /// A One Grab...Transformer component, which should be attached to the grabbable object. Defaults to One Grab Free Transformer.
        /// If you set the Two Grab Transformer property and still want to use one hand for grabs, you must set this property as well.
        /// </summary>
        [Tooltip("A One Grab...Transformer component, which should be attached to the grabbable object. Defaults to One Grab Free Transformer. If you set the Two Grab Transformer property and still want to use one hand for grabs, you must set this property as well.")]
        [SerializeField, Interface(typeof(ITransformer))]
        [Optional(OptionalAttribute.Flag.AutoGenerated)]
        private UnityEngine.Object _oneGrabTransformer = null;

        /// <summary>
        /// A Two Grab...Transformer component, which should be attached to the grabbable object.
        /// If you set this property but also want to use one hand for grabs, you must set the One Grab Transformer property.
        /// </summary>
        [Tooltip("A Two Grab...Transformer component, which should be attached to the grabbable object. If you set this property but also want to use one hand for grabs, you must set the One Grab Transformer property.")]
        [SerializeField, Interface(typeof(ITransformer)), Optional]
        private UnityEngine.Object _twoGrabTransformer = null;

        /// <summary>
        /// Takes a target object to transform instead of transforming the object that has the Grabbable component.
        /// The object with the Grabbable component acts as a controller that projects its transforms onto the target object.
        /// </summary>
        [Tooltip("The target transform of the Grabbable. If unassigned, " +
            "the transform of this GameObject will be used.")]
        [SerializeField]
        [Optional(OptionalAttribute.Flag.AutoGenerated)]
        private Transform _targetTransform;

        /// <summary>
        /// The maximum number of grab points. Can be either -1 (unlimited), 1, or 2.
        /// </summary>
        [Tooltip("The maximum number of grab points. Can be either -1 (unlimited), 1, or 2.")]
        [SerializeField]
        private int _maxGrabPoints = -1;

        public int MaxGrabPoints
        {
            get
            {
                return _maxGrabPoints;
            }
            set
            {
                _maxGrabPoints = value;
            }
        }

        public Transform Transform => _targetTransform;
        public List<Pose> GrabPoints => _selectingPoints;

        private ITransformer _activeTransformer = null;
        private ITransformer OneGrabTransformer;
        private ITransformer TwoGrabTransformer;

        protected override void Awake()
        {
            base.Awake();
            OneGrabTransformer = _oneGrabTransformer as ITransformer;
            TwoGrabTransformer = _twoGrabTransformer as ITransformer;
        }

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());

            if (_targetTransform == null)
            {
                _targetTransform = transform;
            }

            if (_oneGrabTransformer != null)
            {
                this.AssertField(OneGrabTransformer, nameof(OneGrabTransformer));
                OneGrabTransformer.Initialize(this);
            }

            if (_twoGrabTransformer != null)
            {
                this.AssertField(TwoGrabTransformer, nameof(TwoGrabTransformer));
                TwoGrabTransformer.Initialize(this);
            }

            // Create a default if no transformers assigned
            if (OneGrabTransformer == null &&
                TwoGrabTransformer == null)
            {
                OneGrabFreeTransformer defaultTransformer = gameObject.AddComponent<OneGrabFreeTransformer>();
                _oneGrabTransformer = defaultTransformer;
                OneGrabTransformer = defaultTransformer;
                OneGrabTransformer.Initialize(this);
            }

            this.EndStart(ref _started);
        }

        public override void ProcessPointerEvent(PointerEvent evt)
        {
            switch (evt.Type)
            {
                case PointerEventType.Select:
                    EndTransform();
                    break;
                case PointerEventType.Unselect:
                    ForceMove(evt);
                    EndTransform();
                    break;
                case PointerEventType.Cancel:
                    EndTransform();
                    break;
            }

            base.ProcessPointerEvent(evt);

            switch (evt.Type)
            {
                case PointerEventType.Select:
                    BeginTransform();
                    break;
                case PointerEventType.Unselect:
                    BeginTransform();
                    break;
                case PointerEventType.Move:
                    UpdateTransform();
                    break;
            }
        }

        private void ForceMove(PointerEvent releaseEvent)
        {
            PointerEvent moveEvent = new PointerEvent(releaseEvent.Identifier,
                PointerEventType.Move, releaseEvent.Pose, releaseEvent.Data);
            ProcessPointerEvent(moveEvent);
        }

        // Whenever we change the number of grab points, we save the
        // current transform data
        private void BeginTransform()
        {
            // End the transform on any existing transformer before we
            // begin the new one
            EndTransform();

            int useGrabPoints = _selectingPoints.Count;
            if (_maxGrabPoints != -1)
            {
                useGrabPoints = Mathf.Min(useGrabPoints, _maxGrabPoints);
            }

            switch (useGrabPoints)
            {
                case 1:
                    _activeTransformer = OneGrabTransformer;
                    break;
                case 2:
                    _activeTransformer = TwoGrabTransformer;
                    break;
                default:
                    _activeTransformer = null;
                    break;
            }

            if (_activeTransformer == null)
            {
                return;
            }

            _activeTransformer.BeginTransform();
        }

        private void UpdateTransform()
        {
            if (_activeTransformer == null)
            {
                return;
            }

            _activeTransformer.UpdateTransform();
        }

        private void EndTransform()
        {
            if (_activeTransformer == null)
            {
                return;
            }
            _activeTransformer.EndTransform();
            _activeTransformer = null;
        }

        protected override void OnDisable()
        {
            if (_started)
            {
                EndTransform();
            }

            base.OnDisable();
        }

        #region Inject

        public void InjectOptionalOneGrabTransformer(ITransformer transformer)
        {
            _oneGrabTransformer = transformer as UnityEngine.Object;
            OneGrabTransformer = transformer;
        }

        public void InjectOptionalTwoGrabTransformer(ITransformer transformer)
        {
            _twoGrabTransformer = transformer as UnityEngine.Object;
            TwoGrabTransformer = transformer;
        }

        public void InjectOptionalTargetTransform(Transform targetTransform)
        {
            _targetTransform = targetTransform;
        }

        #endregion
    }
}