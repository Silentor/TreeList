using System;
using UnityEditor;
using UnityEngine;

public class TestSerPropPersistency : MonoBehaviour
{
    public Int32[] Arr = new [] { 1, 2, 3 };

    // Start is called before the first frame update
    void Start()
    {
        var so = new SerializedObject( this );
        var arrProp = so.FindProperty( "Arr" );
        var firstElement = arrProp.GetArrayElementAtIndex( 0 ).Copy();

        Debug.Log( $"path {firstElement.propertyPath}, value {firstElement.intValue}" );

        arrProp.InsertArrayElementAtIndex( 0 );
        arrProp.GetArrayElementAtIndex( 0 ).intValue = 0;
        so.ApplyModifiedProperties();

        Debug.Log( $"path {firstElement.propertyPath}, value {firstElement.intValue}" );
        Debug.Log( $"path {arrProp.GetArrayElementAtIndex( 0 ).propertyPath}, value {arrProp.GetArrayElementAtIndex( 0 ).intValue}" );
    }

}
