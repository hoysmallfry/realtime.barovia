using UnityEngine;
using UnityEditor;
 
public class Object2Terrain : EditorWindow
{
    #region Types
    private delegate void CleanUp();
    #endregion

    #region Static Methods
    [MenuItem("Terrain/Object to Terrain", false, 2000)]
	private static void OpenWindow ()
	{
		EditorWindow.GetWindow<Object2Terrain>(true);
	}
    #endregion

    #region Static Fields
    private static readonly string[] bottomTopRadio = new string[] { "Bottom Up", "Top Down" };
    #endregion

    #region Fields
    private int resolution = 512;
	
	private Vector3 addTerrain;

	private int bottomTopRadioSelected = 0;
	
	private float shiftHeight = 0f;
    #endregion

    #region Methods
    private void OnGUI ()
	{
		resolution = EditorGUILayout.IntField("Resolution", resolution);
		
		addTerrain = EditorGUILayout.Vector3Field("Add terrain", addTerrain);
		
		shiftHeight = EditorGUILayout.Slider("Shift height", shiftHeight, -1f, 1f);
		
		bottomTopRadioSelected = GUILayout.SelectionGrid(bottomTopRadioSelected, bottomTopRadio, bottomTopRadio.Length, EditorStyles.radioButton);

		using (var disabledGroup = new EditorGUI.DisabledGroupScope(true))
		{
			EditorGUILayout.ObjectField("Selected Object", Selection.activeGameObject, typeof(GameObject));
		}

		using (var disabledGroup = new EditorGUI.DisabledGroupScope(Selection.activeGameObject == null))
		{
			if (!GUILayout.Button("Create Terrain"))
			{
				return;
			}
		}

		CreateTerrain();
	}

	private void CreateTerrain()
	{	
		// Fire up the progress bar
		ShowProgressBar(1, 100);
 
		// Create a terrain.
		TerrainData terrain = new TerrainData();
		
		// Set up the resolution of the height map.
		terrain.heightmapResolution = resolution;

		// Create the game object from the terrain object.
		GameObject terrainObject = Terrain.CreateTerrainGameObject(terrain);
 
		Undo.RegisterCreatedObjectUndo(terrainObject, "Object to Terrain");
 
		// Get the mesh collider from the selected object.
		MeshCollider collider = Selection.activeGameObject.GetComponent<MeshCollider>();

		CleanUp cleanUp = null;
 
		// If there is no collider, then:
		if (!collider)
		{
			//Add a collider to our source object if it does not exist. Otherwise, raycasting doesn't work.
			collider = Selection.activeGameObject.AddComponent<MeshCollider>();

			// Create an lambda that will delete the mesh collider.
			cleanUp = () => DestroyImmediate(collider);
		}
 
		// Get the bounds of the mesh collider.
		Bounds bounds = collider.bounds;	

		float sizeFactor = collider.bounds.size.y / (collider.bounds.size.y + addTerrain.y);
		
		terrain.size = collider.bounds.size + addTerrain;
		
		bounds.size = new Vector3(terrain.size.x, collider.bounds.size.y, terrain.size.z);
 
		// Do raycasting samples over the object to see what terrain heights should be
		float[,] heights = new float[terrain.heightmapResolution, terrain.heightmapResolution];	
		Ray ray = new Ray(new Vector3(bounds.min.x, bounds.max.y + bounds.size.y, bounds.min.z), -Vector3.up);
		RaycastHit hit = new RaycastHit();
		float meshHeightInverse = 1 / bounds.size.y;
		Vector3 rayOrigin = ray.origin;
 
		int maxHeight = heights.GetLength(0);
		int maxLength = heights.GetLength(1);
 
		Vector2 stepXZ = new Vector2(bounds.size.x / maxLength, bounds.size.z / maxHeight);
 
		for(int zCount = 0; zCount < maxHeight; zCount++)
		{
			ShowProgressBar(zCount, maxHeight);
 
			for(int xCount = 0; xCount < maxLength; xCount++)
			{
				float height = 0.0f;
 
				if(collider.Raycast(ray, out hit, bounds.size.y * 3))
				{
					height = (hit.point.y - bounds.min.y) * meshHeightInverse;
					height += shiftHeight;
 
					//bottom up
					if(bottomTopRadioSelected == 0){
 
						height *= sizeFactor;
					}
 
					//clamp
					if(height < 0)
					{
						height = 0;
					}
				}
 
				heights[zCount, xCount] = height;
           		rayOrigin.x += stepXZ[0];
           		ray.origin = rayOrigin;
			}
 
			rayOrigin.z += stepXZ[1];
      		rayOrigin.x = bounds.min.x;
      		ray.origin = rayOrigin;
		}
 
		terrain.SetHeights(0, 0, heights);
 
		EditorUtility.ClearProgressBar();
 
		if(cleanUp != null)
		{
			cleanUp();    
		}
	}

	private void ShowProgressBar(float progress, float maxProgress)
	{
		float percentage = progress / maxProgress;

		EditorUtility.DisplayProgressBar("Creating Terrain...", $"{Mathf.RoundToInt(percentage * 100f)}%", percentage);
	}
    #endregion
}