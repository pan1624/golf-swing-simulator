using UnityEngine;

public class GolfBallPhysics : MonoBehaviour
{
    public Rigidbody rb;
    private Terrain terrain;
    private TerrainData terrainData;
    private int activeTextureIndex = -1;

    // 針對 Terrain Layers 設定不同的摩擦影響（確保數值在 0.7 - 1 之間）
    public float grassFriction ;  // 草地（低摩擦，球滾較遠）
    public float greenFriction; // 果嶺（超低摩擦，球滾動距離最遠）
    public float roadFriction;   // 道路（較高摩擦，球滾動較短）
    public float sandFriction;   // 沙地（高摩擦，球滾動最慢）
    


    public float groundDrag ; // 控制球的額外阻力，讓減速更自然
   


    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.linearDamping = 0.5f; // 線性阻尼
        terrain = Terrain.activeTerrain;
        terrainData = terrain.terrainData;
    }

    void FixedUpdate()
    {
        DetectTerrainLayer();
        ApplyFriction();
        rb.linearDamping = 0.02f; // 🏌️‍♂️ **確保 Unity 不會自動還原**

    }



    void DetectTerrainLayer()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 2f))
        {
            Vector3 worldPos = hit.point; // 擷取球當前的世界座標
            Vector3 terrainPos = worldPos - terrain.transform.position;

            int mapX = Mathf.FloorToInt(terrainPos.x / terrainData.size.x * terrainData.alphamapWidth);
            int mapZ = Mathf.FloorToInt(terrainPos.z / terrainData.size.z * terrainData.alphamapHeight);

            float[,,] splatmapData = terrainData.GetAlphamaps(mapX, mapZ, 1, 1);

            int maxIndex = 0;
            float maxValue = 0;

            for (int i = 0; i < splatmapData.GetLength(2); i++)
            {
                if (splatmapData[0, 0, i] > maxValue)
                {
                    maxValue = splatmapData[0, 0, i];
                    maxIndex = i;
                }
            }

            activeTextureIndex = maxIndex;
        }
    }

    void ApplyFriction()
    {
        float frictionFactor = 1.0f; // 預設值，無摩擦影響

        if (activeTextureIndex == 0) // 假設 Layer 0 是 Grass_Layer（草地）
            frictionFactor = grassFriction;
        else if (activeTextureIndex == 1) // 假設 Layer 1 是 Green（果嶺）
            frictionFactor = greenFriction;
        else if (activeTextureIndex == 2) // 假設 Layer 2 是 Road（道路）
            frictionFactor = roadFriction;
        else if (activeTextureIndex == 3) // 假設 Layer 3 是 NewLayer（沙地）
            frictionFactor = sandFriction;

        // 施加摩擦影響，確保速度不會立即歸零
        rb.linearVelocity *= frictionFactor;

       
        // 增加 Drag 來讓速度逐漸減小，但不會突然變慢
        rb.linearDamping = groundDrag;
    }
}
