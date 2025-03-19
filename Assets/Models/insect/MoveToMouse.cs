using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;

public class MoveToMouse : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public float speed;
    private Vector3 target;
    private bool selected = false;
    public static List<MoveToMouse> movableObjects = new List<MoveToMouse>();

    void Start()
    {
        movableObjects.Add(this);
        target = transform.position;
    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetMouseButtonDown(1) && selected)
        {
            // Convert screen position to world position
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // Cast ray against colliders and get the hit point
            if (Physics.Raycast(ray, out hit))
            {
                // Set target to the hit position, but maintain current Y position
                target = hit.point;
                target.y = transform.position.y; // Maintain current height

                Debug.Log($"Moving to position: {target}");
            }
        }
        // Move towards the target position
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
        transform.rotation = Quaternion.LookRotation(target - transform.position);

    }
    private void OnMouseDown()
    {
        selected = !selected;
        gameObject.GetComponent<Renderer>().material.color = selected ? Color.red : Color.white;

        foreach (var item in movableObjects)
        {
            if(item != this)
            {
                item.selected = false;
                item.gameObject.GetComponent<Renderer>().material.color = Color.white;
            }
        }
    }
}
