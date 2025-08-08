# 📌 Accès Client

> **Accès Client** est une application **WPF moderne** permettant de centraliser et gérer **tous les accès et ressources d’un parc client** : connexions RDS, VPN, AnyDesk, fichiers partagés et identifiants sécurisés.
> Son objectif : **réduire le temps d’accès** aux ressources, tout en assurant **sécurité et traçabilité**.

---

## 🖼️ Aperçu visuel
<img width="1286" height="743" alt="image_2025-08-08_193049405" src="https://github.com/user-attachments/assets/a7af87d8-6723-43ca-b19c-f2ddbcc32e7a" />
---

## 🚀 Fonctionnalités

### 🗂️ Gestion des clients et ressources

* Organisation **par client** dans une barre latérale
* Classement des éléments par **type** :

  * 🖥️ **RDS** : Connexions Bureau à distance
  * 🌐 **VPN** : Ouverture via FortiClient ou autre
  * 📡 **AnyDesk** : Connexion directe avec mot de passe
  * 📁 **Fichiers/Dossiers** : Ouverture rapide
  * 🔑 **Mots de passe supplémentaires** : panneau dédié à droite

---

### 🔑 Gestion sécurisée des identifiants

* Chiffrement **AES** via `EncryptionHelper`
* Boutons 📋 pour copie rapide de l’utilisateur ou mot de passe
* Toast visuel de confirmation *(pas d’alerte bloquante)*
* Conservation automatique des mots de passe existants lors de l’édition

---

### ▶️ Lancement direct de connexions

* **RDS** :

  * Fichier `.rdp` temporaire avec titre personnalisé
  * Multi-écran activable/désactivable
  * Stockage temporaire sécurisé des identifiants
* **AnyDesk** :

  * Exécution avec `--with-password` via MDPass
  * Validation automatique du chemin (persisté dans `settings.json`)
* **VPN / Fichier / Dossier** :

  * Ouverture/exécution immédiate depuis l’interface

---

### 📡 Gestion des bases partagées

* Fichiers `.antclient` avec verrou `.lock` pour éviter les conflits
* Import d’une base locale avec fusion Clients/Files
* **Save / Save As** activés seulement quand une base est ouverte

---

### 🎛️ Options d’affichage

* Case **Multi-moniteur** pour RDS
* Case **Afficher identifiants** (masque/affiche les champs sensibles)
* Interface adaptative :

  * Colonne centrale limitée en largeur
  * Panneau *Mots de passe supplémentaires* toujours visible
  * Redimensionnement fluide

---

## 📥 Installation

### 1️⃣ Prérequis

* **Windows 10+**
* **.NET 8.0** ou plus récent
* Droits d’exécution d’applications locales

### 2️⃣ Étapes d’installation

```bash
# Télécharger la dernière release depuis GitHub
# Extraire le ZIP
# Lancer :
AccesClient.exe
```

---

## 📚 Utilisation

### ➕ Ajouter un client

<img width="686" height="493" alt="image_2025-08-08_193626438" src="https://github.com/user-attachments/assets/06cc31d8-9706-46b0-a3e0-8b4e233fdf0d" />

1. Cliquer sur **Ajouter client**
2. Remplir les champs
3. Enregistrer

---

### 📂 Ajouter un élément

<img width="686" height="512" alt="image_2025-08-08_193746206" src="https://github.com/user-attachments/assets/bbedd63f-6304-4637-a213-33d6bfa79da3" />

1. Sélectionner le client
2. Cliquer sur **+ Ajouter**
3. Choisir le **type**
4. Renseigner les infos et valider

---

### ▶️ Lancer une connexion

* **Double-cliquer** sur l’élément (RDS, AnyDesk, VPN, Fichier)
* La connexion ou l’ouverture démarre immédiatement avec les paramètres enregistrés

---

### 📋 Copier un identifiant

* Cliquer sur 📋 → Toast visuel en haut à droite

---

## 🛠️ Architecture technique

```plaintext
AccesClient/
│
├── Views/                # Fenêtres et contrôles WPF
├── ViewModels/           # Logique MVVM
├── Models/               # Objets métiers (Client, File, etc.)
├── Converters/           # Conversions XAML (bool → visibility, decrypt, etc.)
├── Helpers/              # Utilitaires (chiffrement, presse-papiers…)
├── Resources/            # Icônes et styles
└── database.json         # Base locale (ou .antclient pour partagé)
```

---

## 📌 Historique des versions

| Version   | Date       | Changements clés                                                     |
| --------- | ---------- | -------------------------------------------------------------------- |
| **1.4.3** | 2025-08-08 | Adaptation UI, suppression largeurs fixes, marges optimisées         |
| **1.4.2** | 2025-08-08 | Panneau *Mots de passe supplémentaires*, commandes MVVM, toast copie |
| **1.4.1** | 2025-08-07 | Ajout gestion AnyDesk, RDS multi-écran, fusion base partagée         |

---

## 🤝 Contributions

1. Forker le dépôt
2. Créer une branche :

   ```bash
   git checkout -b feature/nouvelle-fonctionnalite
   ```
3. Commit :

   ```bash
   git commit -m "Ajout : nouvelle fonctionnalité"
   ```
4. Push :

   ```bash
   git push origin feature/nouvelle-fonctionnalite
   ```
5. Créer une **Pull Request**

---

## 📜 Licence

**MIT License** — Utilisation libre avec attribution

---

## 👨‍💻 Auteur

* **Rodrigue Antunes Barata** — Développeur principal

