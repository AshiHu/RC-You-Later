using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Grappin : MonoBehaviour
{
    [Header("Références")]
    [SerializeField] private RemoteControlCarController controleurVoiture;
    [SerializeField] private Transform pointAttacheCorde;
    [SerializeField] private LineRenderer affichageCorde;
    [SerializeField] private LayerMask couchesAccrochables;

    [Header("Réglages Physique")]
    [SerializeField] private float distanceMaximale = 50f;
    [SerializeField] private float forceTension = 100f;
    [SerializeField] private float amortissementTension = 50f;
    [SerializeField] private float vitesseRotationAuto = 15f;

    private Rigidbody corpsPhysique;
    private ConfigurableJoint articulationCorde;
    private Vector3 pointImpactGrappin;
    private bool estEnTrainDeGrappiner;

    // Accesseur pour savoir de l'extérieur si le grappin est actif
    public bool EstActif => estEnTrainDeGrappiner;

    void Awake()
    {
        corpsPhysique = GetComponent<Rigidbody>();
        if (affichageCorde == null) affichageCorde = GetComponentInChildren<LineRenderer>();
    }

    void Update()
    {
        // On utilise les entrées souris classiques
        if (Input.GetMouseButtonDown(0)) LancerGrappin();
        if (Input.GetMouseButtonUp(0)) ArreterGrappin();
    }

    void FixedUpdate()
    {
        if (estEnTrainDeGrappiner)
        {
            OrienterVoitureVersMouvement();
        }
    }

    void LateUpdate()
    {
        DessinerCorde();
    }

    private void LancerGrappin()
    {
        Ray rayonSortant = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(rayonSortant, out RaycastHit infoImpact, distanceMaximale, couchesAccrochables))
        {
            estEnTrainDeGrappiner = true;
            pointImpactGrappin = infoImpact.point;

            // On coupe le script de pilotage pour ne pas interférer avec la physique du grappin
            if (controleurVoiture != null) controleurVoiture.enabled = false;

            // Création de l'articulation physique
            articulationCorde = gameObject.AddComponent<ConfigurableJoint>();
            articulationCorde.autoConfigureConnectedAnchor = false;
            articulationCorde.connectedAnchor = pointImpactGrappin;

            // On laisse la liberté de mouvement pour que les ressorts (Drive) tirent l'objet
            articulationCorde.xMotion = ConfigurableJointMotion.Free;
            articulationCorde.yMotion = ConfigurableJointMotion.Free;
            articulationCorde.zMotion = ConfigurableJointMotion.Free;

            JointDrive reglageRessort = new JointDrive
            {
                positionSpring = forceTension,
                positionDamper = amortissementTension,
                maximumForce = float.MaxValue
            };

            articulationCorde.xDrive = reglageRessort;
            articulationCorde.yDrive = reglageRessort;
            articulationCorde.zDrive = reglageRessort;

            articulationCorde.targetPosition = Vector3.zero;

            if (affichageCorde != null) affichageCorde.positionCount = 2;
        }
    }

    private void ArreterGrappin()
    {
        if (!estEnTrainDeGrappiner) return;

        estEnTrainDeGrappiner = false;

        if (controleurVoiture != null) controleurVoiture.enabled = true;
        if (affichageCorde != null) affichageCorde.positionCount = 0;
        if (articulationCorde != null) Destroy(articulationCorde);
    }

    private void OrienterVoitureVersMouvement()
    {
        // On récupère la direction actuelle de la voiture
        Vector3 velocite = corpsPhysique.linearVelocity;
        velocite.y = 0; // On ignore la hauteur pour la rotation

        if (velocite.sqrMagnitude > 0.1f)
        {
            Quaternion rotationCible = Quaternion.LookRotation(velocite.normalized, Vector3.up);
            corpsPhysique.MoveRotation(Quaternion.Slerp(corpsPhysique.rotation, rotationCible, Time.fixedDeltaTime * vitesseRotationAuto));
        }

        // On stabilise la rotation pour éviter que la voiture ne tourbillonne
        corpsPhysique.angularVelocity = Vector3.Lerp(corpsPhysique.angularVelocity, Vector3.zero, Time.fixedDeltaTime * 5f);
    }

    private void DessinerCorde()
    {
        if (!estEnTrainDeGrappiner || affichageCorde == null) return;

        affichageCorde.SetPosition(0, pointAttacheCorde.position);
        affichageCorde.SetPosition(1, pointImpactGrappin);
    }
}