using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class BlockDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;

    private Vector2 dragOffset;
    private Transform originalParent;
    private Vector2 originalPosition;

    public bool isDragging = false;

    private List<RectTransform> attachedBlocks = new List<RectTransform>();

    public GameObject ghostPrefab;
    private GameObject ghostInstance;

    public AudioClip snapSound;

    private Dictionary<RectTransform, Vector3> originalWorldPositions = new Dictionary<RectTransform, Vector3>();

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private Dictionary<RectTransform, Transform> originalParents = new Dictionary<RectTransform, Transform>();

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!Input.GetMouseButton(1)) return; // Right click only

        isDragging = true;

        // Detach the dragged block from its parent if parent is another block (not canvas)
        if (transform.parent != null && transform.parent != canvas.transform)
        {
            SetParentKeepWorldPosition(rectTransform, canvas.transform);
        }

        // Now cache attached blocks under dragged block (it will only get children snapped to it)
        CacheAttachedBlocks();

        // Cache original parents for all attached blocks
        originalParents.Clear();
        foreach (var block in attachedBlocks)
        {
            originalParents[block] = block.parent;
        }

        if (ghostPrefab != null)
        {
            if (ghostInstance != null)
                Destroy(ghostInstance);

            ghostInstance = Instantiate(ghostPrefab, canvas.transform);
            ghostInstance.SetActive(true); // let UpdateGhostPreview hide it

            // Safely assign image sprite from dragged block to ghost
            Image blockImage = GetComponent<Image>();
            Image ghostImage = ghostInstance.GetComponent<Image>();
            ghostImage.enabled = true;
            ghostImage.color = new Color(1, 1, 1, 0.5f); // semi-transparent

            if (blockImage != null && ghostImage != null)
            {
                ghostImage.sprite = blockImage.sprite;
                ghostImage.color = new Color(blockImage.color.r, blockImage.color.g, blockImage.color.b, 0.4f); // transparent
                ghostImage.preserveAspect = true;
            }
            else
            {
                Debug.LogWarning("Missing Image component on either the dragged block or ghost prefab.");
            }

            // Optional: make sure ghost does not block raycasts
            CanvasGroup ghostGroup = ghostInstance.GetComponent<CanvasGroup>();
            if (ghostGroup != null)
            {
                ghostGroup.blocksRaycasts = false;
            }
            if (ghostPrefab != null && ghostInstance == null)
            {
                ghostInstance = Instantiate(ghostPrefab, canvas.transform);
            }
        }

        // Cache original world positions for dragging
        originalWorldPositions.Clear();
        foreach (var block in attachedBlocks)
        {
            originalWorldPositions[block] = block.position;
        }

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.6f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        Vector3 mouseWorldPos;
        RectTransformUtility.ScreenPointToWorldPointInRectangle(canvas.transform as RectTransform, eventData.position, eventData.pressEventCamera, out mouseWorldPos);

        Vector3 dragDelta = mouseWorldPos - originalWorldPositions[rectTransform];

        foreach (var block in attachedBlocks)
        {
            block.position = originalWorldPositions[block] + dragDelta;
        }

        UpdateGhostPreview();
        canvasGroup.blocksRaycasts = false;
    }


    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        isDragging = false;

        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        if (ghostInstance != null)
        {
            Destroy(ghostInstance);
        }

        TrySnap();

        if (ghostInstance != null)
        {
            Destroy(ghostInstance);
            ghostInstance = null;
        }
    }

    private Vector2 GetMouseLocalPosition(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint
        );
        return localPoint;
    }

    private void CacheAttachedBlocks()
    {
        attachedBlocks.Clear();
        attachedBlocks.Add(rectTransform); // always include self

        // Recursively collect all children currently parented to dragged block
        void CollectChildren(Transform parent)
        {
            foreach (Transform child in parent)
            {
                if (child.CompareTag("Block"))
                {
                    RectTransform childRT = child.GetComponent<RectTransform>();
                    attachedBlocks.Add(childRT);
                    CollectChildren(child);
                }
            }
        }

        CollectChildren(rectTransform);
    }
    private void UpdateGhostPreview()
    {
        if (ghostInstance == null) return;

        Image blockImage = GetComponent<Image>();
        Image ghostImage = ghostInstance.GetComponent<Image>();

        ghostImage.sprite = blockImage.sprite;
        ghostImage.color = new Color(1, 1, 1, 0.5f); // semi-transparent
        ghostImage.enabled = true;
        ghostImage.preserveAspect = true;
        ghostImage.raycastTarget = false;

        GameObject snapTarget = null;
        Vector3 snapPos = Vector3.zero;

        float snapThresholdY = 0.3f; // world units
        float snapThresholdX = 0.4f;

        GameObject[] allBlocks = GameObject.FindGameObjectsWithTag("Block");

        foreach (GameObject blockObj in allBlocks)
        {
            if (blockObj == gameObject) continue;

            RectTransform otherRT = blockObj.GetComponent<RectTransform>();
            Vector3 delta = otherRT.position - rectTransform.position;

            float heightScaled = rectTransform.rect.height * rectTransform.lossyScale.y;

            // Check if this block is just above dragged block (within threshold)
            if (Mathf.Abs(delta.y - heightScaled) < snapThresholdY && Mathf.Abs(delta.x) < snapThresholdX)
            {
                snapTarget = blockObj;

                // Snap position: right below the snap target block
                snapPos = otherRT.position - new Vector3(0, heightScaled, 0);
                break;
            }
        }

        if (snapTarget != null)
        {
            ghostInstance.SetActive(true);
            ghostInstance.transform.SetParent(snapTarget.transform.parent, false);

            RectTransform ghostRT = ghostInstance.GetComponent<RectTransform>();
            RectTransform targetRT = snapTarget.GetComponent<RectTransform>();

            // Calculate total stack height starting from snapTarget
            float totalHeight = 0f;
            List<RectTransform> stackBlocks = new List<RectTransform>();

            void CollectStack(RectTransform blockRT)
            {
                stackBlocks.Add(blockRT);
                foreach (Transform child in blockRT)
                {
                    if (child.CompareTag("Block"))
                    {
                        CollectStack(child.GetComponent<RectTransform>());
                    }
                }
            }

            CollectStack(targetRT);

            foreach (var blockRT in stackBlocks)
            {
                totalHeight += blockRT.rect.height * blockRT.lossyScale.y;
            }

            // Set ghost size to match total height and width of the dragged block
            ghostRT.sizeDelta = new Vector2(targetRT.rect.width, totalHeight);

            // Position the ghost so its top aligns with snapTarget’s bottom
            Vector3 pos = targetRT.position - new Vector3(0, totalHeight, 0);
            ghostRT.position = pos;
        }
        else
        {
            ghostInstance.SetActive(false);
        }
    }

    private void TrySnap()
    {
        bool snapped = false;

        var allBlocks = GameObject.FindGameObjectsWithTag("Block");

        foreach (var otherObj in allBlocks)
        {
            if (otherObj == gameObject) continue;

            RectTransform otherRT = otherObj.GetComponent<RectTransform>();

            float yGap = otherRT.position.y - rectTransform.position.y;
            float xGap = otherRT.position.x - rectTransform.position.x;

            float heightScaled = rectTransform.rect.height * rectTransform.lossyScale.y;

            bool closeY = Mathf.Abs(yGap - heightScaled) < 0.3f;
            bool closeX = Mathf.Abs(xGap) < 0.4f;

            if (closeY && closeX)
            {
                Vector3 targetWorldPos = otherRT.position - new Vector3(0, heightScaled, 0);

                // Store world offsets of all attached blocks relative to dragged block
                Dictionary<RectTransform, Vector3> worldOffsets = new();
                foreach (var block in attachedBlocks)
                    worldOffsets[block] = block.position - rectTransform.position;

                // Parent dragged block to snap target (the block above)
                SetParentKeepWorldPosition(rectTransform, otherRT);

                // Parent blocks below dragged block as children of dragged block
                foreach (var block in attachedBlocks)
                {
                    if (block == rectTransform) continue;
                    SetParentKeepWorldPosition(block, rectTransform);
                }

                StartCoroutine(SmoothWorldSnapGroup(targetWorldPos, worldOffsets));

                if (snapSound != null)
                    AudioSource.PlayClipAtPoint(snapSound, Camera.main.transform.position);

                snapped = true;
                break;
            }
        }

        if (!snapped)
        {
            // Restore all original parents on failed snap
            foreach (var block in attachedBlocks)
            {
                // Make sure the block was stored before restoring
                if (originalParents.TryGetValue(block, out Transform originalParent))
                {
                    SetParentKeepWorldPosition(block, originalParent);
                }
                else
                {
                    Debug.LogWarning($"Original parent for block {block.name} not found in dictionary.");
                }
            }
        }
    }
    private IEnumerator SmoothSnap(Vector2 targetAnchoredPos)
    {
        float duration = 0.15f;
        float elapsed = 0f;
        Vector2 startPos = rectTransform.anchoredPosition;

        while (elapsed < duration)
        {
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, targetAnchoredPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        rectTransform.anchoredPosition = targetAnchoredPos;
    }

    // Static helper method used everywhere to keep world position on reparenting
    public static void SetParentKeepWorldPosition(RectTransform child, Transform newParent)
    {
        Vector3 worldPos = child.position;
        child.SetParent(newParent, true);
        child.position = worldPos;
    }

    private IEnumerator SmoothWorldSnapGroup(Vector3 targetWorldPos, Dictionary<RectTransform, Vector3> worldOffsets)
    {
        float duration = 0.15f;
        float elapsed = 0f;
        Vector3 startWorldPos = rectTransform.position;

        while (elapsed < duration)
        {
            foreach (var block in attachedBlocks)
            {
                Vector3 start = startWorldPos + worldOffsets[block];
                Vector3 target = targetWorldPos + worldOffsets[block];
                block.position = Vector3.Lerp(start, target, elapsed / duration);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        foreach (var block in attachedBlocks)
        {
            block.position = targetWorldPos + worldOffsets[block];
        }
    }

}

// Marker component for container drop zones
public class DropZone : MonoBehaviour
{
}
