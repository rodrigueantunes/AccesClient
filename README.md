# 📌 Accès Client

Accès Client est une application WPF moderne permettant de centraliser et gérer tous les accès et ressources d’un parc client : connexions RDS, VPN, AnyDesk, fichiers partagés et identifiants sécurisés.

Objectif : réduire le temps d’accès aux ressources, tout en assurant sécurité et traçabilité.

---

## 🖼️ Aperçu visuel

<img width="1286" height="743" alt="image_2025-08-08_193049405" src="https://github.com/user-attachments/assets/a7af87d8-6723-43ca-b19c-f2ddbcc32e7a" />

---

## 🚀 Fonctionnalités

### 🗂️ Gestion des clients et ressources

- Organisation par client dans une barre latérale
- Classement des éléments par type :
  - 🖥️ RDS : connexions Bureau à distance
  - 🌐 VPN : ouverture via FortiClient (ou autre)
  - 📡 AnyDesk : connexion directe (mot de passe optionnel)
  - 📁 Fichiers / Dossiers : ouverture rapide
  - 🗃️ Rangement : dossiers logiques pour regrouper les éléments d’un client
  - 🔑 Mots de passe supplémentaires : panneau dédié à droite

### 🗃️ Rangement (organisation interne)

- Création de rangements par client (dossiers logiques)
- Affectation d’un élément à un rangement lors de l’ajout
- Possibilité de déplacer un élément vers un rangement existant
- Vue filtrable par rangement pour retrouver rapidement une ressource

### 🔑 Gestion sécurisée des identifiants

- Chiffrement AES via `EncryptionHelper`
- Boutons 📋 pour copie rapide de l’utilisateur ou du mot de passe
- Toast visuel de confirmation (pas d’alerte bloquante)
- Conservation automatique des mots de passe existants lors de l’édition

### ▶️ Lancement direct de connexions

- RDS
  - Fichier `.rdp` temporaire avec titre personnalisé
  - Identifiants temporaires gérés de façon sécurisée
  - Multi-écran avancé : choix du nombre d’écrans et sélection des écrans à utiliser
- AnyDesk
  - Exécution avec `--with-password` via MDPass
  - Validation automatique du chemin (persisté dans `settings.json`)
- VPN / Fichier / Dossier
  - Ouverture / exécution immédiate depuis l’interface

### 🖥️ Multi-moniteur (RDS)

- Mode multi-moniteur activable
- Deux modes :
  - Tous les écrans
  - Sélection : choix du nombre d’écrans, puis sélection des écrans souhaités
- Affichage des numéros d’écrans depuis l’application (bouton `N°`) pour faciliter le mapping

### 🔄 Mise à jour automatique

- Au démarrage, l’application peut lancer `AccesClientUpdater` pour vérifier la disponibilité d’une nouvelle version
- Si une mise à jour est disponible, un assistant guide le téléchargement et l’installation (fenêtres de progression dédiées)

### 📡 Gestion des bases partagées

- Fichiers `.antclient` avec verrou `.lock` pour éviter les conflits
- Import d’une base locale avec fusion Clients / Files
- `Save` / `Save As` activés seulement quand une base est ouverte

### 🎛️ Options d’affichage

- Options RDS : multi-moniteur / sélection d’écrans
- Option afficher identifiants (masque / affiche les champs sensibles)
- Interface adaptative
  - Colonne centrale limitée en largeur
  - Panneau Mots de passe supplémentaires toujours visible
  - Redimensionnement fluide

---

## 📥 Installation

### Prérequis

- Windows 10+
- .NET 8.0 (ou plus récent)
- Droits d’exécution d’applications locales

### Étapes

1. Télécharger la dernière release GitHub
2. Extraire le ZIP
3. Lancer `AccesClient.exe`

---

## 📚 Utilisation

### ➕ Ajouter un client

<img width="686" height="493" alt="image_2025-08-08_193626438" src="https://github.com/user-attachments/assets/06cc31d8-9706-46b0-a3e0-8b4e233fdf0d" />

1. Cliquer sur Ajouter client
2. Remplir les champs
3. Enregistrer

### 📂 Ajouter un élément

<img width="686" height="512" alt="image_2025-08-08_193746206" src="https://github.com/user-attachments/assets/bbedd63f-6304-4637-a213-33d6bfa79da3" />

1. Sélectionner le client
2. Cliquer sur + Ajouter
3. Choisir le type
4. Renseigner les infos et valider
5. Optionnel : choisir un rangement

### 🗃️ Créer un rangement

1. Sélectionner le client
2. Cliquer sur + Ajouter
3. Choisir le type Rangement
4. Donner un nom au rangement

### 🧹 Ranger un élément

- Lors de l’ajout : choisir le rangement souhaité
- Pour un élément existant : le déplacer vers un rangement (selon les actions disponibles dans l’interface)

### ▶️ Lancer une connexion

- Double-clic sur l’élément (RDS, AnyDesk, VPN, Fichier)
- La connexion ou l’ouverture démarre avec les paramètres enregistrés

### 📋 Copier un identifiant

- Cliquer sur 📋 → toast visuel en haut à droite

---

## 🛠️ Architecture technique

```plaintext
AccesClient/
│
├── Views/                Fenêtres et contrôles WPF
├── ViewModels/           Logique MVVM
├── Models/               Objets métiers (Client, File, etc.)
├── Converters/           Conversions XAML (bool → visibility, decrypt, etc.)
├── Helpers/              Utilitaires (chiffrement, presse-papiers, version…)
├── Resources/            Icônes et styles
├── AccesClientUpdaterHost/  Hôte de mise à jour (démarrage / téléchargement)
└── database.json         Base locale (ou .antclient pour partagé)
```

---

## 📌 Historique des versions

| Version | Date | Changements clés |
| --- | --- | --- |
| 1.6.2 | 2026-02-21 | Mise à jour automatique au démarrage via AccesClientUpdater, multi-moniteur RDS avec choix du nombre d’écrans et sélection des écrans, ajout du rangement |
| 1.4.3 | 2025-08-08 | Adaptation UI, suppression largeurs fixes, marges optimisées |
| 1.4.2 | 2025-08-08 | Panneau Mots de passe supplémentaires, commandes MVVM, toast copie |
| 1.4.1 | 2025-08-07 | Ajout gestion AnyDesk, RDS multi-écran, fusion base partagée |

---

## 🤝 Contributions

1. Forker le dépôt
2. Créer une branche

```bash
git checkout -b feature/nouvelle-fonctionnalite
```

3. Commit

```bash
git commit -m "Ajout : nouvelle fonctionnalité"
```

4. Push

```bash
git push origin feature/nouvelle-fonctionnalite
```

5. Créer une Pull Request

---

## 📜 Licence

MIT License — utilisation libre avec attribution

---

## 👨‍💻 Auteur

Rodrigue Antunes Barata — développeur principal
