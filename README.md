# Accès Client

Bienvenue dans **Accès Client**, une application .NET (WPF) permettant de centraliser et de faciliter la gestion des accès à distance de vos clients (Bureau à distance, VPN, AnyDesk, accès aux fichiers et dossiers, etc.). Cette solution a été conçue pour simplifier l'ouverture et la maintenance de sessions distantes, ainsi que la gestion d'informations sensibles (login/mot de passe) grâce à un chiffrement intégré.

---

## Sommaire

1. [Fonctionnalités](#fonctionnalités)  
2. [Prérequis & Installation](#prérequis--installation)  
3. [Structure du Projet](#structure-du-projet)  
4. [Configuration](#configuration)  
    - [Fichier `database.json`](#fichier-databasejson)  
    - [Fichier `rds_accounts.json`](#fichier-rds_accountsjson)  
5. [Utilisation](#utilisation)  
6. [Sécurité & Chiffrement](#sécurité--chiffrement)  
7. [Contribuer](#contribuer)  
8. [Licence](#licence)

---

## Fonctionnalités

- **Gestion des Clients**  
  Ajoutez, supprimez, ou modifiez facilement les clients dans l’application. Chaque client peut disposer de plusieurs accès (RDS, VPN, AnyDesk, dossiers, fichiers, etc.).

- **Accès Bureau à distance (RDS)**  
  Ouvrez rapidement une session RDS (via `mstsc.exe`) avec la possibilité d’activer le mode multi-moniteur.

- **Accès AnyDesk**  
  Gérez les identifiants AnyDesk et mot de passe associés. Lancez AnyDesk en un clic, en passant automatiquement le mot de passe si nécessaire.

- **Accès VPN**  
  Lancez votre VPN (FortiClient ou autre exécutable) directement depuis l’application.

- **Ouverture de Dossiers & Fichiers**  
  Ouvrez un dossier (Windows Explorer) ou un fichier (toute extension) en un clic.

- **Gestion centralisée**  
  Un seul fichier JSON (`database.json`) contient toutes les informations sur les clients et leurs accès. Un deuxième JSON optionnel (`rds_accounts.json`) centralise vos différents comptes RDS.

- **Chiffrement**  
  Les mots de passe (RDS, AnyDesk, Windows, etc.) sont stockés en Base64 après un chiffrement AES symétrique (clé fixe dans ce démo).

- **Interface WPF moderne**  
  Une interface utilisateur épurée, avec liste de clients à gauche et liste d’accès à droite, ainsi que des fenêtres de gestion (ajout/édition/suppression) pour une prise en main rapide.

- **Aide en ligne**  
  Possibilité d’accéder à un service d’aide en ligne (extranet, etc.) directement depuis l’application. L’utilisateur peut également définir le navigateur par défaut si l’association `.htm` n’est pas correcte.

---

## Prérequis & Installation

1. **.NET Framework**  
   L’application cible le **.NET Framework 4.8** (ou version ultérieure 4.x). Assurez-vous qu’il soit installé sur votre système.

2. **Télécharger et exécuter la Release**  

---

## Structure du Projet

```plaintext
AccesClientWPF
├─ AccesClientWPF.csproj
├─ Properties/
│  └─ Resources.Designer.cs     # Ressources générées (icônes, images, etc.)
├─ Commands/
│  └─ RelayCommand.cs           # Implémentation d'ICommand pour la MVVM
├─ Converters/
│  ├─ FileTypeToIconConverter.cs # Convertisseur type -> icône
│  ├─ NullOrEmptyToVisibilityConverter.cs
│  └─ StringToVisibilityConverter.cs
├─ Helpers/
│  ├─ DatabaseHelper.cs         # Gestion du fichier JSON principal
│  ├─ EncryptionHelper.cs       # Chiffrement AES (Encrypt/Decrypt)
│  └─ ...
├─ Models/
│  ├─ ClientModel.cs            # Représente un client
│  ├─ FileModel.cs              # Représente un accès/un fichier
│  ├─ DatabaseModel.cs          # Structure globale chargée/sauvegardée dans database.json
│  └─ RdsAccount.cs             # Représente un compte RDS (rds_accounts.json)
├─ Services/
│  ├─ RdsService.cs             # Logique pour lancer MSTSC
│  └─ VpnService.cs             # Éventuellement pour gérer la connexion VPN
├─ ViewModels/
│  ├─ MainViewModel.cs          # Vue-modèle principale (gestion UI + interaction données)
│  ├─ RdsAccountViewModel.cs    # Vue-modèle de la fenêtre RDS Account
│  └─ ...
├─ Views/
│  ├─ MainWindow.xaml (+ .cs)   # Fenêtre principale
│  ├─ AddEntryWindow.xaml (+ .cs)
│  ├─ ClientManagementWindow.xaml (+ .cs)
│  ├─ RdsAccountWindow.xaml (+ .cs)
│  └─ ...
└─ app.config / App.xaml        # Configuration d'application
```

---

## Configuration

### Fichier `database.json`

Ce fichier contient la structure suivante :

```json
{
  "Clients": [
    {
      "Name": "ExempleClient",
      "Path": "..."
    },
    ...
  ],
  "Files": [
    {
      "Name": "ExempleAcces",
      "Type": "RDS",
      "FullPath": "adresse_ip:domaine\\utilisateur:motdepasse_chiffré",
      "Client": "ExempleClient",
      "CustomIconPath": "...",
      "WindowsUsername": "...",
      "WindowsPassword": "..."
    },
    ...
  ]
}
```

- **Clients** : Liste des clients (nom, chemin, etc.).  
- **Files** : Liste des accès. Chaque entrée associe un _Type_ (`RDS`, `VPN`, `AnyDesk`, `Dossier`, `Fichier`), un _FullPath_ (qui peut contenir les identifiants chiffrés pour RDS/AnyDesk), et le nom du client.

> **Note** : Les mots de passe sont chiffrés via [EncryptionHelper](./Helpers/EncryptionHelper.cs) (AES).

Ce fichier est normalement sauvegardé dans le dossier `C:\Application\database.json` selon le code fourni, ou dans `AppDomain.CurrentDomain.BaseDirectory`. Adaptez en fonction de vos besoins.  

### Fichier `rds_accounts.json`

Un second fichier JSON peut exister pour stocker plusieurs comptes RDS :

```json
[
  {
    "Description": "MonServeurRDS",
    "IpDns": "192.168.0.10",
    "NomUtilisateur": "Administrateur",
    "MotDePasse": "abc...xyz (chiffré)",
    "DateCreation": "2025-03-31T12:34:56.0000000"
  },
  ...
]
```

Ce fichier se trouve dans le même dossier que l’application (`rds_accounts.json`). Vous pouvez le gérer depuis l’UI en cliquant sur **Gérer RDS** (si implémenté dans l’interface).

---

## Utilisation

1. **Lancer l’application**  
   À l’exécution, la fenêtre principale s’ouvre, affichant la liste des **Clients** à gauche, et la liste des **éléments** (accès) à droite.  

2. **Sélection d’un Client**  
   - Cliquez sur un client dans la liste de gauche pour afficher tous les accès qui lui sont rattachés (RDS, AnyDesk, etc.).

3. **Double-clic pour se connecter**  
   - Double-cliquez sur un accès **RDS** pour ouvrir MSTSC avec utilisateur/mot de passe pré-renseigné (grâce à `cmdkey`) et éventuellement en **multi-moniteur** (case à cocher `ChkMultiMonitor`).  
   - Double-cliquez sur un accès **AnyDesk** pour lancer AnyDesk.exe et transmettre le mot de passe.  
   - Double-cliquez sur un **VPN** pour lancer le fichier exécutable du VPN.  
   - Double-cliquez sur un **Dossier** pour l’ouvrir dans l’Explorateur Windows.  
   - Double-cliquez sur un **Fichier** pour l’ouvrir (application par défaut).

4. **Ajouter / Gérer Clients**  
   - Cliquez sur **Gérer les clients** pour ouvrir la fenêtre de gestion (ajout, suppression, tri) des clients. Vous pouvez ensuite ajouter des **éléments (accès)** spécifiques à chaque client.

5. **Ajouter / Gérer Accès (Fichiers)**  
   - Dans la fenêtre principale, bouton **+** (ou depuis la fenêtre ClientManagementWindow) pour créer un nouveau lien/accès.  
   - Choisissez le **Type** (`RDS`, `VPN`, etc.), renseignez les champs (mot de passe chiffré automatiquement), et validez.

6. **Modifier / Supprimer**  
   - Sélectionnez un accès (fichier) et utilisez la commande de modification ou de suppression depuis la fenêtre "Éléments Existant" ou depuis l’interface principale (le projet inclut un double-binding pour ces actions).

7. **Copier Identifiants Windows (AnyDesk)**  
   - Pour les connexions AnyDesk, vous pouvez renseigner un couple `WindowsUsername` / `WindowsPassword`. Les boutons 📋 vous permettent de copier ces identifiants dans le presse-papiers en un clic.

8. **Ouvrir l’Extranet / Aide en Ligne**  
   - Des boutons sont disponibles à gauche pour ouvrir votre extranet ou l’aide en ligne. Selon la configuration, l’appli peut vous demander de choisir un navigateur si l’association du `.htm` n’est pas réglée.

---

## Sécurité & Chiffrement

- Les mots de passe sont chiffrés en AES (avec une clé et un IV statiques dans `EncryptionHelper.cs`).  
- Les données chiffrées sont ensuite encodées en Base64, avant d’être stockées dans les fichiers JSON.  
- **Attention** : L’utilisation d’une clé/IV statiques est un exemple simplifié. En production, il est recommandé d’utiliser une solution plus robuste pour la génération et le stockage des clés (gestion de secrets, DPAPI, etc.).

---

## Contribuer

Les contributions sont les bienvenues ! 

1. **Fork** ce dépôt  
2. Créez une **branche feature** : `git checkout -b feature/MonFeature`  
3. **Commitez** vos modifications : `git commit -m 'Ajout de la fonction X'`  
4. **Pushez** la branche : `git push origin feature/MonFeature`  
5. Ouvrez une **Pull Request** 

Merci d’avance pour vos retours, améliorations et propositions de nouvelles fonctionnalités !

---

## Licence

Ce projet est distribué sous licence de votre choix (MIT, Apache, etc.).

---

**Contact & Support**  
Pour toute question, suggestion ou remontée de bug, ouvrez une *Issue* sur GitHub ou contactez-nous directement.

**Merci d’utiliser Accès Client !**
