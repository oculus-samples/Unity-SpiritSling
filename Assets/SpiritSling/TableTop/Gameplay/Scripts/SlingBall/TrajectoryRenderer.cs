// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpiritSling.TableTop
{
    [RequireComponent(typeof(Trajectory))]
    public class TrajectoryRenderer : MonoBehaviour
    {
        /// <summary>
        /// Time gap between each point in the trajectory.
        /// </summary>
        [Range(.01f, 1)]
        [SerializeField]
        private float pointTimeGap = .1f;

        /// <summary>
        /// Number of points to render in the trajectory.
        /// </summary>
        [Range(1, 100)]
        [SerializeField]
        private int pointCount = 30;

        /// <summary>
        /// Layer mask used to detect collisions for rendering the trajectory.
        /// </summary>
        [SerializeField]
        private LayerMask layerMask;

        /// <summary>
        /// Size of each particle in the particle system.
        /// </summary>
        [SerializeField]
        private float particleSize = 1;

        /// <summary>
        /// Flag indicating whether to render on each update frame.
        /// </summary>
        [SerializeField]
        private bool renderOnUpdate;

        /// <summary>
        /// Material used to render each particle
        /// </summary>
        [SerializeField]
        private Material particleMaterial;

        [SerializeField]
        private List<GameObject> ignoreCollisionWith;

        /// <summary>
        /// Reference to the Trajectory component.
        /// </summary>
        private Trajectory trajectory;

        /// <summary>
        /// Particle system used for rendering the trajectory as particles.
        /// </summary>
        private ParticleSystem particles;

        /// <summary>
        /// Array of particles for the particle system.
        /// </summary>
        private ParticleSystem.Particle[] cloud;

        private List<Vector3> points;

        private RaycastHit collisionInfo;

        /// <summary>
        /// Cleanup method called when the object is destroyed.
        /// </summary>
        void OnDestroy()
        {
            //if (particles != null)
            //    Destroy(particles.gameObject); // Destroy the particle system GameObject if it exists
        }

        private void Awake()
        {
            cloud = new ParticleSystem.Particle[pointCount + 1];
            points = new List<Vector3>(pointCount);
            trajectory = GetComponent<Trajectory>(); // Get the Trajectory component
            particles = GetComponentInChildren<ParticleSystem>();
        }

        private void OnEnable()
        {
            collisionInfo = new RaycastHit();
        }

        /// <summary>
        /// Initialization method called on the first frame.
        /// </summary>
        void Start()
        {
            // Initialize the ParticleSystem
            //GameObject pgo = new GameObject("ParticleSystem");
            //pgo.transform.SetParent(transform);
            //particles = pgo.AddComponent<ParticleSystem>();

            particles.Stop();

            var main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = .1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var shape = particles.shape;
            shape.enabled = false;

            var em = particles.emission;
            em.enabled = false;

            //ParticleSystemRenderer r = particles.GetComponent<ParticleSystemRenderer>();
            //r.material = particleMaterial;
            particles.Play();
        }

        public RaycastHit GetCollisionInfo() => collisionInfo;
        public Vector3 GetCollisionPoint() => collisionInfo.point;

        /// <summary>
        /// Called once per frame to optionally render the trajectory.
        /// </summary>
        void Update()
        {
            if (renderOnUpdate)
                Render(); // Render the trajectory if the renderOnUpdate flag is set
        }

        /// <summary>
        /// Renders the trajectory based on the selected render type.
        /// </summary>
        public void Render()
        {
            var pts = GetPoints(); // Get the list of trajectory points

            // Render using particles
            for (var ii = 0; ii < pts.Count; ++ii)
            {
                cloud[ii].position = pts[ii];
                cloud[ii].startSize = particleSize;
                cloud[ii].startColor = particleMaterial.color;
            }

            particles.SetParticles(cloud, pts.Count);
            particles.gameObject.SetActive(true);
        }

        /// <summary>
        /// Generates a list of points representing the trajectory.
        /// </summary>
        /// <returns>List of Vector3 points representing the trajectory.</returns>
        public List<Vector3> GetPoints()
        {
            var curTime = 0f;

            Vector3 newPoint;
            points.Clear();
            for (var i = 0; i < pointCount; i++)
            {
                newPoint = trajectory.GetPointAtTime(curTime); // Get the current point in the trajectory
                var nextPoint = trajectory.GetPointAtTime(curTime + pointTimeGap); // Get the next point in the trajectory
                points.Add(newPoint);
                curTime += pointTimeGap;

                // Stop if there is a collision between the current and next point
                if (Physics.Linecast(newPoint, nextPoint, out var hit, layerMask.value))
                {
                    // Debug.Log("Trajectory collides with " + hit.collider.gameObject + " in " + hit.collider.transform.parent.name);
                    if (ignoreCollisionWith.Contains(hit.collider.gameObject))
                        continue;

                    // A collision has been found
                    //Debug.Log("Collision found at " + GetCollisionPoint() + " " + collisionInfo.point);
                    collisionInfo = hit;
                    points.Add(hit.point);
                    break;
                }
            }

            return points;
        }

        /// <summary>
        /// Hides the rendered trajectory.
        /// </summary>
        public void Hide()
        {
            if (particles != null) particles.gameObject.SetActive(false); // Deactivate particle system if it exists
        }

        public void Show()
        {
            if (particles != null) particles.gameObject.SetActive(true); // Deactivate particle system if it exists
        }

        public int GetPointCount() => points.Count;

        public Vector3 GetPointIndex(int index)
        {
            index = Mathf.Clamp(index, 0, points.Count);
            if (index < points.Count)
                return points[index];

            return Vector3.zero;
        }
    }
}