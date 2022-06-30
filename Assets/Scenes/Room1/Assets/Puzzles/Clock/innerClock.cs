using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InnerClock : MonoBehaviour
{
    [SerializeField]
    private List<CableEnd> cableEndsA, cableEndsB;

    [SerializeField]
    private List<Material> cableMaterials;

    private List<(CableEnd, CableEnd)> desiredCableConnections = new();
    private List<(CableEnd, CableEnd)> connectedCableEnds = new();
    private (CableEnd, CableEnd) currentCableEnds = new();
    public GameObject activeCable { get; private set; }
    private const int totalCableConnections = 3;
    private bool bSingleCableSelected = false;

    // Whether the InnerClock Puzzle is activated
    public bool bActivated { private get; set; } = false;

    const float MAX_CABLE_LENGTH = 10.0f;


    // TODO Cables should be connectable from both sides

    void Start()
    {
        GenerateCableConnections();
    }

    void Update()
    {
        if (bSingleCableSelected)
        {
            // TODO Check with new input system
            Vector3 cursorPosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            
            Vector3 relativeCursorPosition = cursorPosition - transform.position;

            // Hardcode cursor offset, due to object rotation
            // relativeCursorPosition.y = relativeCursorPosition.x;
            relativeCursorPosition.x = relativeCursorPosition.z;
            relativeCursorPosition.z = 0;
            relativeCursorPosition = new Vector3(relativeCursorPosition.x / transform.lossyScale.x,
                                                 relativeCursorPosition.z / transform.lossyScale.x,
                                                 relativeCursorPosition.y / transform.lossyScale.x);

            Vector3 connectionVector = relativeCursorPosition - currentCableEnds.Item1.GetConnectionPosition();
            connectionVector *= connectionVector.magnitude > MAX_CABLE_LENGTH ? (MAX_CABLE_LENGTH / connectionVector.magnitude) : 1;

            DrawConnection(currentCableEnds.Item1, currentCableEnds.Item1.GetConnectionPosition() + connectionVector);
        }
    }

    public delegate void OnCablesConnected();
    public OnCablesConnected onCablesConnectedCallback;

    void InitCableEnds()
    {
        UnityEngine.Assertions.Assert.IsTrue(cableEndsA.Count == cableEndsB.Count);
        
        for (int i = 0; i < cableEndsA.Count; i++)
        {
            cableEndsA[i].connectionClickedDelegate += ReceiveConnectionClicked;
            cableEndsB[i].connectionClickedDelegate += ReceiveConnectionClicked;
        }
    }

    void GenerateCableConnections()
    {
        InitCableEnds();

        List<CableEnd> tempACableEnds = new(cableEndsA);
        List<CableEnd> tempBCableEnds = new(cableEndsB);
        List<Color> generatedColors = new();
        GameManager.ShuffleList(cableMaterials);
        desiredCableConnections.Clear();

        float generatedHue = Random.Range(0.0f, 1.0f);
        for (int i = 1; i <= tempACableEnds.Count; i++)
        {
            float currentHue = (generatedHue + ((float)i / (float)tempACableEnds.Count)) % 1.0f;
            generatedColors.Add(Random.ColorHSV(currentHue, currentHue, 1, 1, 1, 1, 1, 1));
        }

        for (int i = 0; i < generatedColors.Count; i++)
        {
            int cableAIndex = Random.Range(0, tempACableEnds.Count);
            int cableBIndex = Random.Range(0, tempBCableEnds.Count);

            desiredCableConnections.Add((tempACableEnds[cableAIndex], tempBCableEnds[cableBIndex]));

            tempACableEnds[cableAIndex].SetCableEndColor(generatedColors[i]);
            tempBCableEnds[cableBIndex].SetCableEndColor(generatedColors[i]);

            tempACableEnds.RemoveAt(cableAIndex);
            tempBCableEnds.RemoveAt(cableBIndex);
        }
    }

    void ReceiveConnectionClicked(CableEnd cableEnd)
    {
        if (!bActivated || IsConnected(cableEnd))
            return;

        if (currentCableEnds.Item1 == null)
        {
            currentCableEnds.Item1 = cableEnd;
            bSingleCableSelected = true;
        }
        else
        {
            currentCableEnds.Item2 = cableEnd;
            if (!ConnectCableEnds())
            {
                RemoveActiveCable();
            }
        }
    }

    bool IsConnected(CableEnd cableEnd)
    {
        foreach ((CableEnd, CableEnd) connectedPair in connectedCableEnds)
        {
            if (connectedPair.Item1 == cableEnd || connectedPair.Item2 == cableEnd)
            {
                return true;
            }
        }
        return false;
    }

    bool ConnectCableEnds()
    {
        if (VerifyConnection())
        {
            DrawConnection(currentCableEnds.Item1, currentCableEnds.Item2, true);

            connectedCableEnds.Add(currentCableEnds);
            currentCableEnds = new();
            bSingleCableSelected = false;
            VerifyAllCableConnections();
            return true;
        }
        return false;
    }

    void RemoveActiveCable()
    {
        currentCableEnds = new();
        bSingleCableSelected = false;
        Destroy(activeCable);
        activeCable = null;
    }

    void DrawConnection(CableEnd originCable, Vector3 positionB, bool bFinal = false)
    {
        LineRenderer activeLineRenderer = null;

        if (activeCable == null)  // not equal to if (!activeCable)
        {
            activeCable = new GameObject();
            activeCable.transform.parent = transform;
            activeCable.transform.localPosition = Vector3.zero;
            activeCable.transform.localRotation = Quaternion.identity;
            activeCable.transform.localScale = Vector3.one;

            activeLineRenderer = activeCable.AddComponent<LineRenderer>();
            Material cableMaterial = cableMaterials[connectedCableEnds.Count];
            cableMaterial.SetColor("_Color", originCable.cableEndColor);
            activeLineRenderer.material = cableMaterial;

            activeLineRenderer.useWorldSpace = false;
            activeLineRenderer.sortingOrder = 3;
        }

        activeLineRenderer = activeLineRenderer ? activeLineRenderer : activeCable.GetComponent<LineRenderer>();

        Vector3 positionA = originCable.GetConnectionPosition();
        positionA.y = 0.15f;
        positionB.y = 0.15f;
        activeLineRenderer.SetPosition(0, positionA);
        activeLineRenderer.SetPosition(1, positionB);

        activeCable = bFinal ? null : activeCable;
        activeLineRenderer.sortingOrder = bFinal ? 1 : 3;
    }

    void DrawConnection(CableEnd originCable, CableEnd targetCable, bool bFinal = false)
    {
        DrawConnection(originCable, targetCable.GetConnectionPosition(), bFinal);
    }

    bool VerifyConnection((CableEnd, CableEnd) connection)
    {
        UnityEngine.Assertions.Assert.IsTrue(connectedCableEnds.Count <= desiredCableConnections.Count);

        // Check if the connection already exists
        for (int i = 0; i < connectedCableEnds.Count; i++)
        {
            if (connectedCableEnds[i] == desiredCableConnections[i])
            {
                print("Invalid connection");
                return false;
            }
        }

        for (int i = 0; i < desiredCableConnections.Count; i++)
        {
            if (desiredCableConnections[i] == connection ||
                desiredCableConnections[i] == (connection.Item2, connection.Item1))
            {
                print("Found valid connection");
                return true;
            }
        }
        print("Invalid connection");
        return false;
    }

    bool VerifyConnection()
    {
        return VerifyConnection(currentCableEnds);
    }

    void VerifyAllCableConnections()
    {
        if (connectedCableEnds.Count == totalCableConnections)
        {
            GameManager.Instance.bClockWiresSolved = true;
            onCablesConnectedCallback?.Invoke();
        }
    }
}
