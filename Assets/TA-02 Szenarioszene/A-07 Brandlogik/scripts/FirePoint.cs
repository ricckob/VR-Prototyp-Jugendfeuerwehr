using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public enum FireClass
{
    A,
    B,
    C,
    D,
    F
}

// Ein einzelner Brandpunkt auf einem brennbaren Objekt.
// Ablauf: bekommt Hitze von brennenden Nachbarn -> entzündet sich -> wächst -> gibt selbst Hitze ab.
// Gelöscht wird er über ReduceFireIntensity (Feuerlöscher) oder ExtinguishByLid (Deckel).
public class FirePoint : MonoBehaviour
{
    [Header("References")]
    public FlammableObject flammableParent;
    public GameObject vfxObject;
    public VisualEffect vfx;
    public Collider catchRadiusCollider; // nur aktiv solange es brennt

    [Header("Audio")]
    public AudioSource fireLoopSource;
    public AudioSource sparkSource;
    [Range(0.5f, 10f)] public float sparkInterval = 2f;
    [Range(0f, 1f)] public float sparkChance = 0.5f;

    [Header("Fire Class")]
    public FireClass fireClass = FireClass.A;

    public List<FireClass> acceptedIgnitionSources = new List<FireClass> { FireClass.A };

    public bool requiresManualIgnition = false;

    [Header("Lid Extinguishing")]
    public bool canBeExtinguishedByLid = false;
    [Range(0.05f, 5f)] public float lidExtinguishDelay = 1.0f;
    public string tallFireSpawnRateProperty = "TallFireSpawnRate";
    public float tallFireOnValue = 25f;
    public float tallFireOffValue = 0f;

    [Header("Combustibility")]
    [Range(1f, 30f)] public float ignitionTime = 10f;      // so viel Hitze braucht es zum Entzünden
    [Range(0.05f, 2f)] public float heatTransferRate = 0.5f; // so viel Hitze gibt er pro Sekunde ab

    [Header("Fire Growth & Health")]
    [Range(10f, 200f)] public float maxHealth = 100f;     // "Lebenspunkte" des Feuers
    [Range(0.1f, 50f)] public float fireGrowthRate = 20f; // so schnell wächst es pro Sekunde

    [Header("Spread")]
    [Range(0.05f, 5f)] public float spreadRadius = 0.5f;
    [Range(0.05f, 5f)] public float catchRadius = 0.5f;
    [Range(1f, 20f)] public float spreadTickRate = 1f; // wie oft pro Sekunde Hitze verteilt wird

    [Header("Re-Ignition")]
    [Range(0f, 10f)] public float reIgnitionDelay = 3f;      // so lange nach dem Löschen gar nicht neu entzündbar
    [Range(1f, 3f)] public float reIgnitionMultiplier = 1.5f; // danach braucht es mehr Hitze als beim ersten Mal
    [Range(0f, 20f)] public float reducedSpreadDuration = 10f;

    [Header("Extinguish Protection")]
    [Range(0f, 5f)] public float heatBlockAfterExtinguish = 1.25f;

    [Header("VFX - Scale")]
    [Range(0.1f, 1f)] public float minScale = 0.1f;
    [Range(0.5f, 3f)] public float maxScale = 1f;

    [Header("VFX - Fire")]
    public string fireSpawnRateProperty = "FireSpawnRate";
    public string fireSizeProperty = "FireSize";
    [Range(1f, 50f)] public float minFireSpawnRate = 5f;
    [Range(1f, 50f)] public float maxFireSpawnRate = 50f;
    [Range(0.1f, 3f)] public float minFireSize = 0.3f;
    [Range(0.1f, 3f)] public float maxFireSize = 3f;

    [Header("VFX - Smoke")]
    [Range(0f, 5f)] public float smokeLingerTime = 2f; // so lange läuft der Rauch nach dem Löschen weiter

    [Header("VFX - Animation")]
    [Range(1f, 10f)] public float animSpeed = 3f;
    [SerializeField, Range(1f, 20f)] private float vfxUpdateRate = 5f; // VFX nur 5x pro Sekunde anpassen, spart Leistung

    [Header("Debug")]
    public bool showDebug = false;

    public enum BurnState
    {
        Cold,                 // normal, sammelt evtl. Hitze
        RecentlyExtinguished, // gerade gelöscht, schwer wieder zu entzünden
        Cooling               // kühlt langsam ab, danach wieder Cold
    }

    // Von außen nur lesbar, geändert wird nur hier drin
    public BurnState state { get; private set; } = BurnState.Cold;
    public bool isIgnited { get; private set; }
    public float heat { get; private set; }
    public float currentHealth { get; private set; }

    // 0 = gerade entzündet, 1 = volle Größe
    public float lifeProgress => maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;

    // Wird nach jedem Löschen ausgelöst (z.B. für den FireRoundManager im Tutorial)
    public static event System.Action<FirePoint> Extinguished;

    private float timeSinceExtinguished;
    private float heatBlockedUntil;
    private float spreadTimer;
    private float sparkTimer;
    private float vfxTimer;
    private float lastExtinguishHitTime = -1f; // wann zuletzt Löschmittel getroffen hat

    // Aktuelle (geglättete) VFX-Werte
    private float currentScale;
    private float currentSpawnRate;
    private float currentSize;

    // Property-IDs statt Strings, ist schneller beim Setzen
    private int spawnRateId, sizeId, tallFireId;
    private bool hasSpawnRate, hasSize, hasTallFire;

    private Coroutine lidRoutine;

    // Nachbarn im spreadRadius. Werden beim Entzünden einmal gesucht,
    // statt jede Sekunde alle FirePoints der Szene durchzugehen.
    private readonly List<FirePoint> neighbours = new List<FirePoint>();
    private bool neighboursFound;

    // Awake statt Start: läuft direkt beim Instantiate, also bevor jemand Ignite() aufrufen kann
    private void Awake()
    {
        if (flammableParent == null)
            flammableParent = GetComponentInParent<FlammableObject>();

        // Vfx leer -> Visual Effect selbst suchen, auch in Kindobjekten (true = auch inaktive)
        if (vfx == null)
            vfx = (vfxObject != null ? vfxObject : gameObject).GetComponentInChildren<VisualEffect>(true);

        // Vfx Object leer oder = FirePoint selbst -> das Objekt nehmen, auf dem der Visual Effect liegt.
        // Sonst würde z.B. ein Kind "Burning VFX" nie ausgeschaltet und von Anfang an brennen.
        if (vfx != null && (vfxObject == null || vfxObject == gameObject))
            vfxObject = vfx.gameObject;

        if (vfxObject != null)
            currentScale = vfxObject.transform.localScale.x;

        // VFX ausschalten, bis das Feuer brennt
        SetVFXActive(false);

        spawnRateId = Shader.PropertyToID(fireSpawnRateProperty);
        sizeId = Shader.PropertyToID(fireSizeProperty);
        tallFireId = Shader.PropertyToID(tallFireSpawnRateProperty);

        // Nicht jeder VFX Graph hat alle Parameter, deshalb vorher prüfen
        if (vfx != null)
        {
            hasSpawnRate = vfx.HasFloat(spawnRateId);
            hasSize = vfx.HasFloat(sizeId);
            hasTallFire = vfx.HasFloat(tallFireId);
        }

        // Trigger, damit Löschpartikel nicht an einer unsichtbaren Kugel abprallen
        // und die Sichtprüfung vom Feuerlöscher nicht an dieser Kugel hängen bleibt
        if (catchRadiusCollider != null)
        {
            catchRadiusCollider.isTrigger = true;
            if (catchRadiusCollider is SphereCollider sphere)
                sphere.radius = catchRadius;
        }

        if (fireLoopSource != null) fireLoopSource.playOnAwake = false;
        if (sparkSource != null) sparkSource.playOnAwake = false;
    }

    // Start läuft erst nach allen Awake/OnEnable. Hat eine AudioSource mit "Play On Awake"
    // trotzdem schon losgespielt, wird sie hier gestoppt, solange der Punkt nicht brennt.
    private void Start()
    {
        if (isIgnited)
            return;

        if (fireLoopSource != null) fireLoopSource.Stop();
        if (sparkSource != null) sparkSource.Stop();
    }

    private void OnEnable()
    {
        FirePointManager.Register(this);
        UpdateCatchCollider();
    }

    // Wird auch beim Zerstören aufgerufen, deshalb reicht OnDisable (kein OnDestroy nötig)
    private void OnDisable()
    {
        FirePointManager.Unregister(this);
        StopFire();
        state = BurnState.Cold;
        HideVFX();
    }

    private void Update()
    {
        // Kalte Punkte ohne Hitze haben nichts zu tun -> sofort raus.
        // Das sind die allermeisten, deshalb spart das viel.
        if (!isIgnited && heat <= 0f && state == BurnState.Cold)
            return;

        if (!HasValidOwner())
            return;

        float deltaTime = Time.deltaTime;

        if (isIgnited)
            UpdateBurning(deltaTime);
        else
            UpdateNotBurning(deltaTime);

        // VFX nur ein paar Mal pro Sekunde anpassen
        vfxTimer += deltaTime;
        if (vfxTimer >= 1f / vfxUpdateRate)
        {
            UpdateVFX(vfxTimer);
            vfxTimer = 0f;
        }
    }

    private void UpdateBurning(float deltaTime)
    {
        // Feuer wächst bis maxHealth, aber nicht, während es gerade gelöscht wird.
        // Sonst wächst es bei schwachem Strahl schneller nach, als es gelöscht wird.
        bool beingExtinguished = Time.time - lastExtinguishHitTime < 0.25f;
        if (!beingExtinguished)
            currentHealth = Mathf.Min(currentHealth + fireGrowthRate * deltaTime, maxHealth);

        // Hitze an Nachbarn abgeben
        spreadTimer += deltaTime;
        if (spreadTimer >= 1f / spreadTickRate)
        {
            SpreadHeat(spreadTimer);
            spreadTimer = 0f;
        }

        UpdateSparks(deltaTime);
    }

    private void UpdateNotBurning(float deltaTime)
    {
        if (state == BurnState.RecentlyExtinguished)
        {
            // Nach einer Weile geht "gerade gelöscht" in "abkühlen" über
            timeSinceExtinguished += deltaTime;
            if (timeSinceExtinguished >= reducedSpreadDuration)
                state = BurnState.Cooling;
        }
        else if (state == BurnState.Cooling)
        {
            // Hitze langsam abbauen, bei 0 wieder ganz normal
            heat = Mathf.Max(0f, heat - 5f * deltaTime);
            if (heat <= 0f)
            {
                state = BurnState.Cold;
                timeSinceExtinguished = 0f;
            }
        }

        TryAutoIgnite();
    }

    // Entzündet den Punkt, wenn genug Hitze gesammelt wurde
    private void TryAutoIgnite()
    {
        if (requiresManualIgnition || heat <= 0f || Time.time < heatBlockedUntil)
            return;

        float requiredHeat = ignitionTime;

        // Schon mal gelöscht: erst nach reIgnitionDelay und mit mehr Hitze wieder entzündbar
        if (state != BurnState.Cold)
        {
            if (timeSinceExtinguished < reIgnitionDelay)
                return;

            requiredHeat *= reIgnitionMultiplier;
        }

        if (heat >= requiredHeat)
            Ignite();
    }

    private void UpdateSparks(float deltaTime)
    {
        if (sparkSource == null || sparkSource.clip == null)
            return;

        sparkTimer += deltaTime;
        if (sparkTimer < sparkInterval)
            return;

        sparkTimer = 0f;

        // Nicht jedes Mal knistern, sonst klingt es zu gleichmäßig
        if (Random.value <= sparkChance)
        {
            sparkSource.pitch = Random.Range(0.9f, 1.1f);
            sparkSource.PlayOneShot(sparkSource.clip);
        }
    }

    // elapsed = Zeit seit dem letzten VFX-Update (nicht nur ein Frame!),
    // sonst würde das Feuer bei 5 Updates pro Sekunde viel zu langsam wachsen
    private void UpdateVFX(float elapsed)
    {
        if (vfxObject == null)
            return;

        float t = lifeProgress;
        float smoothing = Mathf.Clamp01(elapsed * animSpeed);

        currentScale = Mathf.Lerp(currentScale, Mathf.Lerp(minScale, maxScale, t), smoothing);
        vfxObject.transform.localScale = Vector3.one * currentScale;

        if (vfx == null)
            return;

        if (hasSpawnRate)
        {
            currentSpawnRate = Mathf.Lerp(currentSpawnRate, Mathf.Lerp(minFireSpawnRate, maxFireSpawnRate, t), smoothing);
            vfx.SetFloat(spawnRateId, currentSpawnRate);
        }

        if (hasSize)
        {
            currentSize = Mathf.Lerp(currentSize, Mathf.Lerp(minFireSize, maxFireSize, t), smoothing);
            vfx.SetFloat(sizeId, currentSize);
        }
    }

    public void Ignite()
    {
        if (isIgnited || !isActiveAndEnabled || !HasValidOwner())
            return;

        isIgnited = true;
        currentHealth = Mathf.Max(currentHealth, 1f);
        timeSinceExtinguished = 0f;
        spreadTimer = 0f;
        sparkTimer = Random.Range(0f, sparkInterval); // damit nicht alle Punkte gleichzeitig knistern
        neighboursFound = false; // Objekt könnte bewegt worden sein, Nachbarn neu suchen

        UpdateCatchCollider();

        SetVFXActive(true);
        if (vfx != null) vfx.Play();
        if (fireLoopSource != null && fireLoopSource.clip != null && !fireLoopSource.isPlaying)
            fireLoopSource.Play();

        // Objekt verkohlen lassen und Brandstelle markieren
        if (flammableParent != null)
            flammableParent.OnFirePointIgnited(this);

        if (FirePointManager.Instance != null)
            FirePointManager.Instance.RegisterBurnedFirePoint(this);

        if (showDebug)
            Debug.Log($"[{name}] entzündet.", this);
    }

    public void Extinguish()
    {
        if (!isIgnited)
            return;

        StopFire();
        state = BurnState.RecentlyExtinguished;
        heatBlockedUntil = Time.time + heatBlockAfterExtinguish; // kurz gar keine Hitze annehmen

        // Rauch noch etwas weiterlaufen lassen, dann VFX aus
        Invoke(nameof(HideVFX), smokeLingerTime);

        if (showDebug)
            Debug.Log($"[{name}] gelöscht.", this);

        Extinguished?.Invoke(this);
    }

    // Wird vom Feuerlöscher jeden Frame beim Treffen aufgerufen
    public void ReduceFireIntensity(float damage)
    {
        if (!isIgnited || damage <= 0f)
            return;

        lastExtinguishHitTime = Time.time;
        currentHealth = Mathf.Max(0f, currentHealth - damage);

        if (currentHealth <= 0f)
            Extinguish();
    }

    // Wird von brennenden Nachbarn aufgerufen
    public void ReceiveHeat(float amount, FireClass sourceClass)
    {
        if (amount <= 0f || isIgnited || !isActiveAndEnabled || !HasValidOwner() || Time.time < heatBlockedUntil)
            return;

        // Nur bestimmte Brandklassen dürfen diesen Punkt entzünden (leere Liste = alle)
        if (acceptedIgnitionSources != null && acceptedIgnitionSources.Count > 0 && !acceptedIgnitionSources.Contains(sourceClass))
            return;

        // Gerade gelöscht -> nimmt nur 30% der Hitze an
        if (state == BurnState.RecentlyExtinguished)
            amount *= 0.3f;

        // Deckel nach oben, damit ein Punkt nicht unendlich Hitze sammelt
        heat = Mathf.Min(heat + amount, ignitionTime * 2f);
    }

    // Deckel auf die Pfanne: Stichflamme aus, nach lidExtinguishDelay gelöscht
    public void ExtinguishByLid()
    {
        if (!isIgnited || !canBeExtinguishedByLid || lidRoutine != null)
            return;

        SetTallFire(tallFireOffValue);
        lidRoutine = StartCoroutine(LidRoutine());
    }

    // Deckel wieder runter, bevor das Feuer aus war
    public void CancelLidExtinguish()
    {
        if (lidRoutine == null)
            return;

        StopLidRoutine();

        if (isIgnited)
            SetTallFire(tallFireOnValue);
    }

    private IEnumerator LidRoutine()
    {
        yield return new WaitForSeconds(lidExtinguishDelay);
        lidRoutine = null; // vor Extinguish, weil das StopLidRoutine aufruft
        Extinguish();
    }

    private void StopLidRoutine()
    {
        if (lidRoutine == null)
            return;

        StopCoroutine(lidRoutine);
        lidRoutine = null;
    }

    private void SetTallFire(float value)
    {
        if (vfx != null && hasTallFire)
            vfx.SetFloat(tallFireId, value);
    }

    // Gibt Hitze an alle Nachbarn im spreadRadius ab, je näher desto mehr
    private void SpreadHeat(float elapsed)
    {
        if (!neighboursFound)
            FindNeighbours();

        Vector3 myPosition = transform.position;

        foreach (FirePoint neighbour in neighbours)
        {
            if (neighbour == null || neighbour.isIgnited)
                continue;

            float distance = Vector3.Distance(myPosition, neighbour.transform.position);
            float falloff = Mathf.InverseLerp(spreadRadius, 0f, distance); // 1 = direkt daneben, 0 = am Rand
            neighbour.ReceiveHeat(heatTransferRate * falloff * elapsed, fireClass);
        }
    }

    private void FindNeighbours()
    {
        neighbours.Clear();
        neighboursFound = true;

        if (FirePointManager.Instance == null)
            return;

        float radiusSquared = spreadRadius * spreadRadius;

        foreach (FirePoint other in FirePointManager.Instance.GetAllFirePoints())
        {
            if (other != this && (other.transform.position - transform.position).sqrMagnitude <= radiusSquared)
                neighbours.Add(other);
        }
    }

    // Alles, was beim Löschen und beim Deaktivieren gleich ist
    private void StopFire()
    {
        isIgnited = false;
        heat = 0f;
        currentHealth = 0f;
        timeSinceExtinguished = 0f;
        spreadTimer = 0f;

        UpdateCatchCollider();
        StopLidRoutine();
        CancelInvoke();

        if (fireLoopSource != null) fireLoopSource.Stop();
        if (sparkSource != null) sparkSource.Stop();
    }

    private void HideVFX()
    {
        // Falls es in der Zwischenzeit wieder angegangen ist
        if (isIgnited)
            return;

        if (vfx != null) vfx.Stop();
        SetVFXActive(false);
    }

    // Schaltet das VFX an/aus.
    // Liegt das VFX auf einem Kindobjekt, wird das ganze Objekt deaktiviert.
    // Liegt es direkt auf dem FirePoint, darf der nicht deaktiviert werden (sonst schaltet er sich
    // selbst ab und kann nie mehr brennen). Dann wird nur die Visual-Effect-Komponente ausgeschaltet.
    // vfx.Stop() allein reicht nicht, weil sich der VFX Graph beim Aktivieren von selbst startet.
    private void SetVFXActive(bool active)
    {
        if (vfxObject != null && vfxObject != gameObject)
            vfxObject.SetActive(active);
        else if (vfx != null)
            vfx.enabled = active;
    }

    private void UpdateCatchCollider()
    {
        if (catchRadiusCollider != null)
            catchRadiusCollider.enabled = isIgnited;
    }

    // Brennt nur, solange das Objekt, zu dem er gehört, aktiv ist
    private bool HasValidOwner()
    {
        return flammableParent == null || flammableParent.isActiveAndEnabled;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, spreadRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, catchRadius);
    }

    [ContextMenu("Test Ignite")]
    private void TestIgnite() => Ignite();
}
