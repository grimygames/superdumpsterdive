using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DumpsterCritters : MonoBehaviour
{
    public enum FlyBehaviorType { ChaoticBuzzer, Zoomer, Sitter }

    [Header("Assets")]
    public GameObject critterPrefab; 
    public Sprite flySprite;

    [Header("Global Fly Spawning")]
    public float flySpawnInterval = 0.8f; 
    public float flyWidth = 140f; 
    public float flyHeight = 140f;
    [Range(0f, 1f)] public float flyBuzzJuice = 0.15f; 

    [Header("Swarm Cap Control")]
    public int maxFliesOnScreen = 3; // FIXED: Maximum limit constraint
    private int currentActiveFlies = 0; // Live tracker matrix

    [Header("Movement Profiles")]
    public float standardFlySpeed = 220f;
    public float zoomerFlySpeed = 550f;
    public float escapeFlySpeed = 600f; 
    public float flyLifetime = 5f;

    private RectTransform canvasRect;

    void Start()
    {
        canvasRect = GetComponent<RectTransform>();
        StartCoroutine(MasterFlySpawnerLoop());
    }

    IEnumerator MasterFlySpawnerLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(flySpawnInterval * 0.3f, flySpawnInterval * 1.1f));
            
            // FIXED: Only fire the spawner if we haven't hit our 3-fly cap limit
            if (this.enabled && currentActiveFlies < maxFliesOnScreen) 
            {
                SpawnAmbientFly();
            }
        }
    }

    void SpawnAmbientFly()
    {
        if (critterPrefab == null || flySprite == null) return;

        // Increment active tracking layer register
        currentActiveFlies++;

        GameObject fly = Instantiate(critterPrefab, transform);
		
        if (fly.TryGetComponent(out RawImage raw)) raw.texture = flySprite.texture;
        else if (fly.TryGetComponent(out Image img)) img.sprite = flySprite;
		
        RectTransform rt = fly.GetComponent<RectTransform>();
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, flyWidth);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, flyHeight);
        rt.localScale = Vector3.one;

        FlyBehaviorType assignedBehavior = (FlyBehaviorType)Random.Range(0, 3);
        fly.name = $"Ambient_Fly_{assignedBehavior}";

        float w = canvasRect.rect.width / 2f;
        float h = canvasRect.rect.height / 2f;

        if (assignedBehavior == FlyBehaviorType.Zoomer)
        {
            Vector2 offScreenPos = Vector2.zero;
            int edge = Random.Range(0, 4);
            switch (edge)
            {
                case 0: offScreenPos = new Vector2(-w - flyWidth, Random.Range(-h, h)); break; 
                case 1: offScreenPos = new Vector2(w + flyWidth, Random.Range(-h, h)); break;  
                case 2: offScreenPos = new Vector2(Random.Range(-w, w), -h - flyHeight); break; 
                case 3: offScreenPos = new Vector2(Random.Range(-w, w), h + flyHeight); break;  
            }
            rt.anchoredPosition3D = new Vector3(offScreenPos.x, offScreenPos.y, -100f);
            StartCoroutine(AnimateZoomerFly(rt, edge));
        }
        else
        {
            rt.anchoredPosition3D = new Vector3(Random.Range(-w * 0.7f, w * 0.7f), Random.Range(-h * 0.7f, h * 0.7f), -100f);
            
            if (assignedBehavior == FlyBehaviorType.ChaoticBuzzer)
                StartCoroutine(AnimateChaoticFly(rt));
            else
                StartCoroutine(AnimateSitterFly(rt));
        }

        rt.SetAsLastSibling();
    }

    // --- BEHAVIOR 1: THE CHAOTIC BUZZER ---
    IEnumerator AnimateChaoticFly(RectTransform rt)
    {
        float elapsed = 0f;
        float personalLifetime = flyLifetime + Random.Range(-1f, 1f); 
        Vector2 flyDirection = Random.insideUnitCircle.normalized; 
        float changeDirTimer = 0f;
        float fadeDuration = 0.5f;

        Graphic flyGraphic = rt.GetComponent<Graphic>();

        while (elapsed < personalLifetime && rt != null)
        {
            elapsed += Time.deltaTime;
            changeDirTimer += Time.deltaTime;

            if (changeDirTimer > Random.Range(0.2f, 0.4f))
            {
                flyDirection = (flyDirection + Random.insideUnitCircle * 1.6f).normalized;
                changeDirTimer = 0f;
            }

            Vector2 noise = new Vector2(Mathf.Sin(Time.time * 35f), Mathf.Cos(Time.time * 30f)) * 5f;
            Vector3 lastPos = rt.anchoredPosition3D;

            rt.anchoredPosition3D += (Vector3)(flyDirection * standardFlySpeed * Time.deltaTime) + (Vector3)noise;
            rt.anchoredPosition3D = new Vector3(rt.anchoredPosition3D.x, rt.anchoredPosition3D.y, -100f);

            ApplyRotation(rt, rt.anchoredPosition3D - lastPos, -90f);
            ApplyWingPulse(rt);

            if (flyGraphic != null && elapsed > (personalLifetime - fadeDuration))
            {
                FadeGraphicAlpha(flyGraphic, personalLifetime - elapsed, fadeDuration);
            }

            yield return null;
        }

        // FIXED: Safely clean out register tracking right before the instance drops out of memory
        currentActiveFlies = Mathf.Max(0, currentActiveFlies - 1);
        if (rt != null) Destroy(rt.gameObject);
    }

    // --- BEHAVIOR 2: THE ZOOMER (IN-AND-OUT) ---
    IEnumerator AnimateZoomerFly(RectTransform rt, int startingEdge)
    {
        float w = canvasRect.rect.width / 2f;
        float h = canvasRect.rect.height / 2f;
        Vector2 travelDirection = Vector2.right;

        switch (startingEdge)
        {
            case 0: travelDirection = Vector2.right; break;
            case 1: travelDirection = Vector2.left; break;
            case 2: travelDirection = Vector2.up; break;
            case 3: travelDirection = Vector2.down; break;
        }

        while (rt != null && 
               rt.anchoredPosition.x >= -w - flyWidth * 2f && rt.anchoredPosition.x <= w + flyWidth * 2f &&
               rt.anchoredPosition.y >= -h - flyHeight * 2f && rt.anchoredPosition.y <= h + flyHeight * 2f)
        {
            Vector3 lastPos = rt.anchoredPosition3D;
            Vector2 sweepNoise = Vector2.zero;

            if (startingEdge == 0 || startingEdge == 1)
                sweepNoise.y = Mathf.Sin(Time.time * 8f) * 5f; 
            else
                sweepNoise.x = Mathf.Cos(Time.time * 8f) * 5f; 

            rt.anchoredPosition3D += (Vector3)(travelDirection * zoomerFlySpeed * Time.deltaTime) + (Vector3)sweepNoise;
            rt.anchoredPosition3D = new Vector3(rt.anchoredPosition3D.x, rt.anchoredPosition3D.y, -100f);

            ApplyRotation(rt, rt.anchoredPosition3D - lastPos, -90f);
            ApplyWingPulse(rt);

            yield return null;
        }

        // FIXED: Safely clean out register tracking right before the instance drops out of memory
        currentActiveFlies = Mathf.Max(0, currentActiveFlies - 1);
        if (rt != null) Destroy(rt.gameObject);
    }

    // --- BEHAVIOR 3: THE INSTANT TAKE-OFF FLY ---
    IEnumerator AnimateSitterFly(RectTransform rt)
    {
        float elapsed = 0f;
        float stateTimer = 0f;
        float fadeDuration = 0.5f;
        float personalLifetime = flyLifetime + Random.Range(-0.5f, 1.5f);
        
        Vector2 flyDirection = Random.insideUnitCircle.normalized;
        Graphic flyGraphic = rt.GetComponent<Graphic>();

        float initialBuzzDuration = Random.Range(0.8f, 1.5f);

        // STAGE 1: INITIAL APPROACH SWEEP
        while (stateTimer < initialBuzzDuration && rt != null)
        {
            elapsed += Time.deltaTime;
            stateTimer += Time.deltaTime;
            Vector2 noise = new Vector2(Mathf.Sin(Time.time * 35f), Mathf.Cos(Time.time * 30f)) * 5f;
            Vector3 lastPos = rt.anchoredPosition3D;

            rt.anchoredPosition3D += (Vector3)(flyDirection * standardFlySpeed * Time.deltaTime) + (Vector3)noise;
            rt.anchoredPosition3D = new Vector3(rt.anchoredPosition3D.x, rt.anchoredPosition3D.y, -100f);

            ApplyRotation(rt, rt.anchoredPosition3D - lastPos, -90f);
            ApplyWingPulse(rt);
            yield return null;
        }

        // STAGE 2: THE "TOUCH AND GO" TAKE-OFF
        flyDirection = Random.insideUnitCircle.normalized; 
        
        while (elapsed < personalLifetime && rt != null)
        {
            elapsed += Time.deltaTime;

            Vector2 escapeNoise = new Vector2(Mathf.Sin(Time.time * 20f), Mathf.Cos(Time.time * 20f)) * 2f; 
            Vector3 lastPos = rt.anchoredPosition3D;

            rt.anchoredPosition3D += (Vector3)(flyDirection * escapeFlySpeed * Time.deltaTime) + (Vector3)escapeNoise;
            rt.anchoredPosition3D = new Vector3(rt.anchoredPosition3D.x, rt.anchoredPosition3D.y, -100f);

            ApplyRotation(rt, rt.anchoredPosition3D - lastPos, -90f);
            ApplyWingPulse(rt);

            if (flyGraphic != null && elapsed > (personalLifetime - fadeDuration))
            {
                FadeGraphicAlpha(flyGraphic, personalLifetime - elapsed, fadeDuration);
            }
            yield return null;
        }

        // FIXED: Safely clean out register tracking right before the instance drops out of memory
        currentActiveFlies = Mathf.Max(0, currentActiveFlies - 1);
        if (rt != null) Destroy(rt.gameObject);
    }

    // --- SHARED MATH HELPERS ---
    private void ApplyRotation(RectTransform rt, Vector3 velocity, float offsetAngle)
    {
        if (velocity.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            rt.localRotation = Quaternion.Euler(0, 0, angle + offsetAngle);
        }
    }

    private void ApplyWingPulse(RectTransform rt)
    {
        float pulseScale = 1f + Mathf.Sin(Time.time * 60f) * flyBuzzJuice;
        rt.localScale = new Vector3(pulseScale, pulseScale, 1f);
    }

    private void FadeGraphicAlpha(Graphic g, float timeRemaining, float duration)
    {
        Color c = g.color;
        c.a = Mathf.Clamp01(timeRemaining / duration);
        g.color = c;
    }
}