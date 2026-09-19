using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public enum MirrorType
{
    Real,    // กระจกจริง
    Fake     // กระจกหลอก
}

public enum MirrorState
{
    Inactive,    // ยังไม่ได้เปิดใช้
    Active,      // เปิดใช้แล้ว
    Damaged,     // เสียหาย (สำหรับกระจกหลอก)
    Completed    // ใช้งานเสร็จแล้ว
}

[System.Serializable]
public class MirrorData
{
    public int mirrorID;
    public MirrorType type;
    public MirrorState state;
    public float currentRotation;
    public float targetRotation;  // มุมที่ถูกต้องสำหรับแก้ puzzle
    public Vector3 position;
    public bool isInteractable;
    public float damageAmount = 1f;  // ความเสียหายที่ได้รับเมื่อระเบิด
}

public class MirrorLightPuzzle : MonoBehaviour
{
    [Header("Puzzle Settings")]
    public int totalMirrors = 10;
    public int realMirrors = 5;
    public int fakeMirrors = 5;
    public float rotationSpeed = 45f;
    public float detectionDistance = 2f;
    public LayerMask mirrorLayer = 1;

    [Header("Light System")]
    public Transform lightSource;
    public Transform sunSymbol;
    public LineRenderer lightBeam;
    public Material lightMaterial;
    public float lightWidth = 0.1f;
    public int maxReflections = 10;

    [Header("Audio")]
    public AudioClip mirrorRotateSound;
    public AudioClip explosionSound;
    public AudioClip successSound;
    public AudioSource audioSource;

    [Header("Effects")]
    public GameObject explosionEffect;
    public GameObject successEffect;

    private List<MirrorData> mirrors = new List<MirrorData>();
    private List<Transform> mirrorTransforms = new List<Transform>();
    private bool isPuzzleCompleted = false;
    //private bool isPlayerInteracting = false;
    private Transform selectedMirror;
    private int selectedMirrorIndex = -1;
    private Camera playerCamera;

    // สำหรับ Haptic Feedback
    private HapticManager hapticManager;
    private bool isCheckingMirror = false;
    private float mirrorCheckTimer = 0f;
    private const float MIRROR_CHECK_DURATION = 2f; // เวลาในการตรวจสอบกระจก

    void Start()
    {
        hapticManager = HapticManager.Instance;
        playerCamera = Camera.main;
        InitializePuzzle();
        SetupLightBeam();
    }

    void Update()
    {
        HandleMirrorInteraction();
        UpdateLightPath();
        CheckPuzzleCompletion();
    }

    void InitializePuzzle()
    {
        // สร้างข้อมูลกระจกทั้งหมด
        mirrors.Clear();
        
        for (int i = 0; i < totalMirrors; i++)
        {
            MirrorData mirror = new MirrorData
            {
                mirrorID = i,
                type = i < realMirrors ? MirrorType.Real : MirrorType.Fake,
                state = MirrorState.Inactive,
                currentRotation = Random.Range(0f, 360f),
                targetRotation = Random.Range(0f, 360f),
                isInteractable = true
            };
            
            mirrors.Add(mirror);
        }

        // สุ่มตำแหน่งกระจก
        ShuffleMirrors();
        
        // สร้าง GameObject สำหรับกระจกแต่ละบาน
        CreateMirrorObjects();
        
        Debug.Log($"สร้าง Puzzle เสร็จ: กระจกจริง {realMirrors} บาน, กระจกหลอก {fakeMirrors} บาน");
    }

    void ShuffleMirrors()
    {
        // สุ่มลำดับกระจกเพื่อไม่ให้ผู้เล่นรู้ว่าบานไหนจริงบานไหนหลอก
        for (int i = 0; i < mirrors.Count; i++)
        {
            MirrorData temp = mirrors[i];
            int randomIndex = Random.Range(i, mirrors.Count);
            mirrors[i] = mirrors[randomIndex];
            mirrors[randomIndex] = temp;
            
            // อัพเดต ID ใหม่
            mirrors[i].mirrorID = i;
        }
    }

    void CreateMirrorObjects()
    {
        // สร้าง GameObject สำหรับกระจกแต่ละบาน (ในโค้ดจริงควรวางใน Scene)
        mirrorTransforms.Clear();
        
        for (int i = 0; i < mirrors.Count; i++)
        {
            GameObject mirrorObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            mirrorObj.name = $"Mirror_{i}";
            mirrorObj.tag = "Mirror";
            mirrorObj.layer = Mathf.RoundToInt(Mathf.Log(mirrorLayer.value, 2));
            
            // วางตำแหน่งเป็นวงกลม
            float angle = (360f / totalMirrors) * i * Mathf.Deg2Rad;
            float radius = 5f;
            Vector3 position = new Vector3(
                Mathf.Cos(angle) * radius,
                1f,
                Mathf.Sin(angle) * radius
            );
            
            mirrorObj.transform.position = position;
            mirrorObj.transform.rotation = Quaternion.Euler(0, mirrors[i].currentRotation, 0);
            
            // เพิ่ม Collider สำหรับ Interaction
            BoxCollider collider = mirrorObj.GetComponent<BoxCollider>();
            collider.isTrigger = false;
            
            // เพิ่ม Component สำหรับระบุประเภทกระจก
            MirrorController controller = mirrorObj.AddComponent<MirrorController>();
            controller.Initialize(mirrors[i], this);
            
            mirrorTransforms.Add(mirrorObj.transform);
            mirrors[i].position = position;
        }
    }

    void HandleMirrorInteraction()
    {
        if (isPuzzleCompleted) return;

        // ตรวจจับการมองหากระจก
        if (Input.GetButtonDown("Fire1")) // Left Click
        {
            DetectMirrorLook();
        }
        
        // หากกำลังตรวจสอบกระจก
        if (isCheckingMirror && selectedMirrorIndex != -1)
        {
            mirrorCheckTimer += Time.deltaTime;
            
            // ส่ง Haptic Feedback ระหว่างการตรวจสอบ
            SendMirrorCheckHaptic();
            
            // หากตรวจสอบครบเวลาแล้ว
            if (mirrorCheckTimer >= MIRROR_CHECK_DURATION)
            {
                RevealMirrorType();
            }
        }

        // หมุนกระจกด้วยปุ่ม
        if (selectedMirrorIndex != -1 && !isCheckingMirror)
        {
            if (Input.GetKey(KeyCode.Q))
            {
                RotateMirror(-rotationSpeed * Time.deltaTime);
            }
            else if (Input.GetKey(KeyCode.E))
            {
                RotateMirror(rotationSpeed * Time.deltaTime);
            }
        }

        // ยกเลิกการเลือก
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            DeselectMirror();
        }
    }

    void DetectMirrorLook()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, detectionDistance, mirrorLayer))
        {
            if (hit.collider.CompareTag("Mirror"))
            {
                MirrorController mirrorController = hit.collider.GetComponent<MirrorController>();
                if (mirrorController != null)
                {
                    SelectMirror(mirrorController.GetMirrorIndex());
                }
            }
        }
    }

    void SelectMirror(int mirrorIndex)
    {
        if (mirrorIndex < 0 || mirrorIndex >= mirrors.Count) return;
        if (!mirrors[mirrorIndex].isInteractable) return;

        selectedMirrorIndex = mirrorIndex;
        selectedMirror = mirrorTransforms[mirrorIndex];
        
        // เริ่มการตรวจสอบกระจก
        StartMirrorCheck();
        
        Debug.Log($"เลือกกระจก {mirrorIndex} (ประเภท: {mirrors[mirrorIndex].type})");
    }

    void StartMirrorCheck()
    {
        isCheckingMirror = true;
        mirrorCheckTimer = 0f;
        
        // เริ่ม Haptic Pattern สำหรับการตรวจสอบ
        if (hapticManager != null)
        {
            hapticManager.PlayPattern(HapticPatternType.Event);
        }
        
        Debug.Log("เริ่มตรวจสอบกระจก... จับจอยไว้!");
    }

    void SendMirrorCheckHaptic()
    {
        if (hapticManager == null || selectedMirrorIndex == -1) return;

        MirrorData mirror = mirrors[selectedMirrorIndex];
        float progress = mirrorCheckTimer / MIRROR_CHECK_DURATION;
        
        if (mirror.type == MirrorType.Fake)
        {
            // กระจกหลอก: สั่นแรงขึ้นเรื่อยๆ เพื่อเตือนอันตราย
            float dangerIntensity = Mathf.Lerp(0.3f, 1.0f, progress * progress);
            
            // ใช้ Warning pattern หากใกล้จะระเบิด
            if (progress > 0.7f)
            {
                hapticManager.PlayBlockWarning(dangerIntensity);
            }
            else
            {
                // สั่นแบบไม่สม่ำเสมอเพื่อสร้างความไม่แน่นอน
                float randomIntensity = Random.Range(0.4f, dangerIntensity);
                hapticManager.Vibrate(randomIntensity, randomIntensity, 0.1f);
            }
        }
        else
        {
            // กระจกจริง: สั่นอ่อนๆ สม่ำเสมอ
            float safeIntensity = Mathf.Lerp(0.1f, 0.4f, Mathf.Sin(progress * Mathf.PI));
            hapticManager.Vibrate(safeIntensity, safeIntensity, 0.1f);
        }
    }

    void RevealMirrorType()
    {
        if (selectedMirrorIndex == -1) return;
        
        MirrorData mirror = mirrors[selectedMirrorIndex];
        isCheckingMirror = false;
        
        if (mirror.type == MirrorType.Fake)
        {
            // กระจกหลอกระเบิด!
            ExplodeFakeMirror(selectedMirrorIndex);
        }
        else
        {
            // กระจกจริง สามารถใช้งานได้
            ActivateRealMirror(selectedMirrorIndex);
        }
    }

    void ExplodeFakeMirror(int mirrorIndex)
    {
        MirrorData mirror = mirrors[mirrorIndex];
        mirror.state = MirrorState.Damaged;
        mirror.isInteractable = false;
        
        // เอฟเฟค Haptic สำหรับการระเบิด
        if (hapticManager != null)
        {
            hapticManager.PlayPattern(HapticPatternType.Boom);
        }
        
        // เล่นเสียงระเบิด
        if (audioSource && explosionSound)
        {
            audioSource.PlayOneShot(explosionSound);
        }
        
        // แสดงเอฟเฟคระเบิด
        if (explosionEffect && mirrorTransforms[mirrorIndex])
        {
            Instantiate(explosionEffect, mirrorTransforms[mirrorIndex].position, Quaternion.identity);
        }
        
        // ทำลายกระจกหลอก
        if (mirrorTransforms[mirrorIndex])
        {
            mirrorTransforms[mirrorIndex].gameObject.SetActive(false);
        }
        
        // ลด HP ของผู้เล่น (ควรเชื่อมต่อกับระบบ HP ของเกม)
        PlayerTakeDamage(mirror.damageAmount);
        
        DeselectMirror();
        
        Debug.Log($"กระจกหลอก {mirrorIndex} ระเบิด! HP -1");
    }

    void ActivateRealMirror(int mirrorIndex)
    {
        MirrorData mirror = mirrors[mirrorIndex];
        mirror.state = MirrorState.Active;
        
        // เอฟเฟค Haptic สำหรับความสำเร็จ
        if (hapticManager != null)
        {
            hapticManager.PlayPattern(HapticPatternType.Success);
        }
        
        Debug.Log($"กระจกจริง {mirrorIndex} เปิดใช้งานแล้ว! สามารถหมุนได้");
    }

    void RotateMirror(float rotationAmount)
    {
        if (selectedMirrorIndex == -1) return;
        if (mirrors[selectedMirrorIndex].state != MirrorState.Active) return;
        
        // หมุนกระจก
        mirrors[selectedMirrorIndex].currentRotation += rotationAmount;
        mirrors[selectedMirrorIndex].currentRotation = mirrors[selectedMirrorIndex].currentRotation % 360f;
        
        if (mirrorTransforms[selectedMirrorIndex])
        {
            mirrorTransforms[selectedMirrorIndex].rotation = 
                Quaternion.Euler(0, mirrors[selectedMirrorIndex].currentRotation, 0);
        }
        
        // เล่นเสียงหมุน
        if (audioSource && mirrorRotateSound && !audioSource.isPlaying)
        {
            audioSource.PlayOneShot(mirrorRotateSound);
        }
        
        // Haptic Feedback สำหรับการหมุน
        if (hapticManager != null)
        {
            hapticManager.Vibrate(0.2f, 0.2f, 0.1f);
        }
    }

    void UpdateLightPath()
    {
        if (!lightSource || !lightBeam) return;
        
        List<Vector3> lightPath = CalculateLightPath();
        
        // อัพเดต LineRenderer
        lightBeam.positionCount = lightPath.Count;
        for (int i = 0; i < lightPath.Count; i++)
        {
            lightBeam.SetPosition(i, lightPath[i]);
        }
    }

    List<Vector3> CalculateLightPath()
    {
        List<Vector3> path = new List<Vector3>();
        
        Vector3 currentPos = lightSource.position;
        Vector3 currentDir = lightSource.forward;
        
        path.Add(currentPos);
        
        for (int reflection = 0; reflection < maxReflections; reflection++)
        {
            Ray ray = new Ray(currentPos, currentDir);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, mirrorLayer))
            {
                MirrorController mirrorController = hit.collider.GetComponent<MirrorController>();
                
                if (mirrorController != null && mirrorController.GetMirrorData().state == MirrorState.Active)
                {
                    path.Add(hit.point);
                    
                    // คำนวณการสะท้อนแสง
                    Vector3 reflectDir = Vector3.Reflect(currentDir, hit.normal);
                    
                    currentPos = hit.point + reflectDir * 0.01f; // เลื่อนนิดหน่อยเพื่อไม่ให้ติดกับพื้นผิว
                    currentDir = reflectDir;
                }
                else
                {
                    // ถ้าไม่ใช่กระจกที่ใช้งานได้ ให้หยุด
                    path.Add(hit.point);
                    break;
                }
            }
            else
            {
                // หากไม่กระทบอะไร ให้ยิงไปไกลๆ
                path.Add(currentPos + currentDir * 100f);
                break;
            }
        }
        
        return path;
    }

    void CheckPuzzleCompletion()
    {
        if (isPuzzleCompleted) return;
        
        // ตรวจสอบว่าแสงไปถึงสัญลักษณ์พระอาทิตย์หรือไม่
        List<Vector3> lightPath = CalculateLightPath();
        
        if (lightPath.Count > 1)
        {
            Vector3 lastPoint = lightPath[lightPath.Count - 1];
            
            if (sunSymbol && Vector3.Distance(lastPoint, sunSymbol.position) < 0.5f)
            {
                CompletePuzzle();
            }
        }
    }

    void CompletePuzzle()
    {
        isPuzzleCompleted = true;
        
        // เอฟเฟค Haptic สำหรับความสำเร็จ
        if (hapticManager != null)
        {
            StartCoroutine(PlaySuccessHapticSequence());
        }
        
        // เล่นเสียงสำเร็จ
        if (audioSource && successSound)
        {
            audioSource.PlayOneShot(successSound);
        }
        
        // แสดงเอฟเฟคสำเร็จ
        if (successEffect && sunSymbol)
        {
            Instantiate(successEffect, sunSymbol.position, Quaternion.identity);
        }
        
        Debug.Log("🎉 แก้ Puzzle สำเร็จ! แสงถึงสัญลักษณ์พระอาทิตย์แล้ว!");
    }

    IEnumerator PlaySuccessHapticSequence()
    {
        // เล่น Haptic Pattern หลายครั้งสำหรับความสำเร็จ
        for (int i = 0; i < 3; i++)
        {
            hapticManager.PlayPattern(HapticPatternType.Success);
            yield return new WaitForSeconds(0.8f);
        }
    }

    void DeselectMirror()
    {
        selectedMirrorIndex = -1;
        selectedMirror = null;
        isCheckingMirror = false;
        mirrorCheckTimer = 0f;
        
        // หยุด Haptic Feedback
        if (hapticManager != null)
        {
            // หยุดการสั่นทั้งหมด (ถ้ามี method นี้ใน HapticManager)
        }
    }

    void SetupLightBeam()
    {
        if (lightBeam)
        {
            lightBeam.material = lightMaterial;
            lightBeam.startWidth = lightWidth;
            lightBeam.endWidth = lightWidth;
            lightBeam.useWorldSpace = true;
        }
    }

    void PlayerTakeDamage(float damage)
    {
        // เชื่อมต่อกับระบบ Health ของเกม
        // ตัวอย่าง:
        // PlayerHealth.Instance.TakeDamage(damage);
        Debug.Log($"ผู้เล่นได้รับความเสียหาย: {damage}");
    }

    // Public Methods สำหรับการเข้าถึงข้อมูล
    public bool IsPuzzleCompleted() => isPuzzleCompleted;
    public int GetActiveMirrorsCount() => mirrors.FindAll(m => m.state == MirrorState.Active).Count;
    public int GetRemainingFakeMirrors() => mirrors.FindAll(m => m.type == MirrorType.Fake && m.state != MirrorState.Damaged).Count;
}