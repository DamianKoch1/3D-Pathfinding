using System;
using UnityEngine;
using UnityEngine.UI;

namespace Pathfinding
{
    public class Player : MonoBehaviour
    {
        [SerializeField] private Camera cam;
        [SerializeField] private LineRenderer lr;
        [SerializeField] private PathfindingSettings settings;
        [SerializeField] private Text text;
        [SerializeField] private NodesGenerator generator;
        [SerializeField] private Transform start;
        [SerializeField] private Transform goal;

        [SerializeField] private float moveSpeed = 5;
        [SerializeField] private float lookSpeedV = 1;
        [SerializeField] private float lookSpeedH = 1;

        private float pitch;
        private float yaw;

        bool useGrid;

        private void Awake()
        {
            generator.OnInitialize += UpdateText;
            text.text = "Loading...";
        }

        private void Update()
        {
            yaw += lookSpeedH * Input.GetAxis("Mouse X");
            pitch += lookSpeedV * -Input.GetAxis("Mouse Y");
            pitch = Mathf.Clamp(pitch, -90, 90);

            transform.eulerAngles = new Vector3(0, yaw, 0);
            cam.transform.eulerAngles = new Vector3(pitch, yaw, 0);

            transform.position += Time.deltaTime * 60 * moveSpeed * (Input.GetAxis("Horizontal") * transform.right + Input.GetAxis("Vertical") * cam.transform.forward);

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                useGrid = !useGrid;
                UpdateText();
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                settings.algorithm = NextEnumValue(settings.algorithm);
                UpdateText();
            }

            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                settings.heuristic = NextEnumValue(settings.heuristic);
                UpdateText();
            }

            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                settings.costIncrease = NextEnumValue(settings.costIncrease);
                UpdateText();
            }

            if (Input.GetKeyDown(KeyCode.KeypadPlus))
            {
                moveSpeed += 3;
            }

            if (Input.GetKeyDown(KeyCode.KeypadMinus))
            {
                moveSpeed -= 3;
                if (moveSpeed < 1) moveSpeed = 1;
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                start.position = transform.position;
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                goal.position = transform.position;
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                var path = useGrid 
                    ? generator.FindGridPath(start.position, goal.position, settings) 
                    : generator.FindGraphPath(start.position, goal.position, settings);

                lr.positionCount = path.Count;
                lr.SetPositions(path.ToArray());
            }
        }

        private void UpdateText()
        {
            text.text = $" {(useGrid ? "Grid" : "NavMesh")}\n{settings.algorithm}\n{settings.heuristic}\n{settings.costIncrease}";
        }

        private T NextEnumValue<T>(T value) where T : struct
        {
            T[] values = (T[])Enum.GetValues(typeof(T));
            int idx = Array.IndexOf<T>(values, value) + 1;
            return (idx >= values.Length) ? values[0] : values[idx];
        }
    }
}
