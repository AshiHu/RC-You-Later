# RC You Later

Bonjour, j'espère que cet examen sera plus facile à comprendre que le précédant :)

Il se compose en deux parties : une partie strict (10 points) et une partie libre (10 points).

Tu as jusqu'à **Mardi 7 Avril 2026 à 23h59** pour rendre ce devoir.

Comme le dernier devoir, fait un fork de ce projet pour te créer ta copie.

## 1ère Partie : Sauvons le temps !

Une fois le projet forké, téléchargé et ouvert sur ta machine, pense à ouvrir la scène `Main` et vérifier s'il tourne bien.

J'aimerais que le jeu sauvegarde les étapes du temps du joueur une fois qu'il a complété un tour complet.

J'ai créé une méthode `Save` dans le script `Timer` (qui se trouve dans le dossier "Assets/Scripts") qui est appelée une fois que le joueur complète un tour.

1. **5 Points** : Avec ce qu'on a vu en cours, rajoute le code nécessaire dans la méthode `Save` pour sauvegarder les étapes (la variable `steps` qui se trouve ligne 7) dans un fichier nommé **"score.txt"** sous format texte à la racine du projet (peut importe le dossier, tant que tu t'y retrouve). Tu n'es pas obligé d'utiliser la méthode que l'on a vu en cours.
   P.S.: Tu n'es pas obligé de faire un tour complet pour t'assurer que ton code fonctionne : tu peux rajouter un script de test qui appel directement la méthode au lancement de la scène. Pense bien à le retirer à la fin!

J'ai créé une méthode `Load` dans le même script. Elle est appelée automatiquement quand le niveau se charge afin d'afficher le dernier temps sauvegardé avant que le joueur ne se lance.

2. **5 Points** : Avec ce qu'on a vu en cours, rajoute le code nécessaire dans cette méthode afin de charger le dernier temps du joueur qui se trouve dans le fichier **"score.txt"** **si le fichier existe**. Tu n'es pas obligé d'utiliser la méthode que l'on a vu en cours.

Points bonus :

3. **2 Points** : Trouve un moyen de ne sauvegarder le temps (et les étapes) du joueur seulement s'il bat le temps final record déjà sauvegardé.

4. **4 Points** (pour les fans du code): Vu que c'est un score on aimerait le "protéger" afin que des petits malins ne viennent pas modifier le fichier à la main. Trouve un moyen de protéger le fichier. Un simple encryptage en [Base64](https://zetcode.com/csharp/base64/) ou sauvegarder en [Binaire](https://learn.microsoft.com/fr-fr/dotnet/api/system.io.binarywriter?view=net-8.0) peut suffire pour cet exercice.

## 2ème Partie : Ça manque d'un certains *je ne sais quoi*..

Ici c'est roue libre !

1. **10 Points** : J'aimerais que tu rajoute **ce que tu veux** dans le projet afin de l'agrémenter. Met en avant ce que tu aime ou sais faire.

Ça peut-être du code, du décors, des VFX, un autre niveau, de la juiciness en plus, des nouvelles fonctionnalités, des sons (musique, ambiance ou SFX), de la narration (?!), du suspense, bref tout ce qui fait un jeu vidéo !

La seule contrainte est que tu y passe un peu de temps dessus (~1h30) et que tu en soit fier à la fin :)

Ne te fais pas de soucis je serais gentil sur la notation de cette partie là.

Et aussi rajoute à la fin de ce fichier **README.md** ce que tu a rajouté dans le projet (afin que je sois sur de bien évaluer ce que tu a rajouté).

Il y a quelque assets dans le dossier des Models mais tu peux en importer d'autres (je te recommande à nouveau le site de [Kenney](https://kenney.nl/assets)).

Ce que tu a rajouté dans le projet :

- Ajout de foliage arbres et fleurs.

- Modification du level design pour tester la nouvelle mécanique intégrée.

- Nouvelle mécanique de grappin via le clic gauche de la souris :
   L’activation du grappin suspend le script de déplacement de base.
   Le grappin nous tire et nous fait faire un balancier avec la vélocité que l'on avait avant de l'activer.
   
