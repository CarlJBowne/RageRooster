using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public struct PlacementSource
{
    public Vector3 positionData { get; private set; }
    public Quaternion rotationData { get; private set; }
    public Transform Transform { get; private set; }

    public readonly Vector3 Position => Transform != null ? Transform.position : positionData;
    public readonly Quaternion Rotation => Transform != null ? Transform.rotation : rotationData;
    public readonly Vector3 EularAngles => Transform != null ? Transform.eulerAngles : rotationData.eulerAngles;


    public PlacementSource(Vector3 position, Quaternion rotation)
    {
        Transform = null;
        positionData = position;
        rotationData = rotation;
    }
    public PlacementSource(Transform transform)
    {
        Transform = transform;
        positionData = transform.position;
        rotationData = transform.rotation;
    }
    public PlacementSource(Vector3 position, Vector3 eularAngles)
    {
        Transform = null;
        positionData = position;
        rotationData = Quaternion.Euler(eularAngles);
    }
    public PlacementSource(Vector3 positionOnly)
    {
        Transform = null;
        positionData = positionOnly;
        rotationData = Quaternion.identity;
    }

    public static implicit operator PlacementSource(Transform transform) => new(transform);
    public static implicit operator PlacementSource(Vector3 position) => new(position);
    public static implicit operator PlacementSource((Vector3 position, Quaternion rotation) data) => new(data.position, data.rotation);
    public static implicit operator PlacementSource((Vector3 position, Vector3 eularAngles) data) => new(data.position, data.eularAngles);
    public static implicit operator Vector3(PlacementSource source) => source.Position;
    public static implicit operator Quaternion(PlacementSource source) => source.Rotation;
    public static implicit operator Transform(PlacementSource source) => source.Transform;
}