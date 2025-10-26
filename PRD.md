# PRD - Générateur de rendu Liquid + JSON avec aperçu en direct

## Nom du produit
Liquid JSON Playground

## Objectif
Offrir une interface interactive pour éditer simultanément :
- un document JSON (modèle de données),
- un template Liquid (modèle de rendu),
et visualiser le **résultat rendu en direct** via **Fluid.Core**.

## Description générale
L’application permet de créer, tester et visualiser des templates Liquid alimentés par des données JSON.  
L’utilisateur saisit ou colle son JSON et son template Liquid.  
L’application convertit le JSON en objet C# (via `System.Text.Json`), puis utilise **Fluid.Core** pour parser et rendre le template Liquid.  
Le résultat est affiché dans la troisième colonne, selon le format choisi (texte brut, XML, SQL, etc.).

## Cas d’usage exemples

### Ex. 0
**Entrée JSON :**
```json
{ "firstName": "John", "lastName": "Doe" }
```
**Template Liquid :**
```
<user>
  <name>{{ firstName }} {{ lastName }}</name>
</user>
```
**Rendu :**
```xml
<user>
  <name>John Doe</name>
</user>
```

### Ex. 1
**Entrée JSON :**
```json
[
  { "type": "string", "name": "FirstName" },
  { "type": "int", "name": "Age" }
]
```
**Template Liquid :**
```
public class Person {
{% for prop in model %}
  public {{ prop.type }} {{ prop.name }} { get; set; }
{% endfor %}
}
```
**Rendu :**
```csharp
public class Person {
  public string FirstName { get; set; }
  public int Age { get; set; }
}
```

### Ex. 2
**Entrée JSON :**
```json
[
  { "Table": "Users", "Values": { "Id": 1, "Name": "John" } },
  { "Table": "Users", "Values": { "Id": 2, "Name": "Jane" } }
]
```
**Template Liquid :**
```
{% for row in model %}
INSERT INTO {{ row.Table }} ({{ row.Values | keys | join: ", " }})
VALUES ({{ row.Values | values | join: ", " }});
{% endfor %}
```
**Rendu :**
```sql
INSERT INTO Users (Id, Name)
VALUES (1, John);
INSERT INTO Users (Id, Name)
VALUES (2, Jane);
```

---

## Interface utilisateur

### Structure générale (3 colonnes)
| Colonne | Contenu | Fonctionnalités principales |
|----------|----------|-----------------------------|
| 1 | **Monaco Editor (JSON)** | - Coloration syntaxique JSON<br>- Validation du JSON<br>- Exemple importable |
| 2 | **Monaco Editor (Liquid)** | - Coloration Liquid<br>- Auto-indent<br>- Exemples de templates |
| 3 | **Zone d’aperçu (rendu)** | - Résultat de la fusion JSON + Liquid, Sélecteur du type d’aperçu (Texte brut, XML, SQL, C#, HTML), Actualisation automatique |

---

## Fonctionnalités détaillées

### 1. Édition
- Monaco Editor pour JSON (langage : `"json"`)
- Monaco Editor pour Liquid (langage : `"liquid"`)
- Thèmes clairs / sombres
- Validation JSON automatique

### 2. Rendu
  1. Parser le JSON → objet C#
  2. Compiler le template Liquid avec **Fluid.Core**
  3. Retourne rle rendu textuel
- L’aperçu affiche le résultat dans un `<pre>` avec syntax highlighting selon le langage choisi.

### 3. Sélecteur de langage d’aperçu
- Dropdown : `Plain Text`, `XML`, `C#`, `SQL`, `HTML`
- Applique un formatage syntaxique de sortie (Monaco).

---

## Technologies
- **Frontend** : Blazor WebAssembly .NET 9.0
  - Librairie : [`BlazorMonaco`](https://github.com/serdarciplak/BlazorMonaco)  
  - Composants : 3 colonnes responsives, select pour format de sortie  
- **Backend** : C# .NET 8 Web API  
  - Librairie : `Fluid.Core` pour le rendu Liquid  
  - `System.Text.Json` pour désérialisation du JSON

---

## Critères de succès
- JSON et Liquid modifiables en parallèle.  
- Rendu instantané (<1s pour objets simples).  
- Coloration syntaxique fluide (Monaco).  
- Aucun plantage en cas d’erreur de parsing.  
- Application hébergeable sur GitHub Pages.
- GitHub Action deploy.yml

---

## MVP attendu
- 3 colonnes fonctionnelles.  
- Conversion JSON → objet C#.  
- Parsing Liquid via Fluid.Core.  
- Aperçu en direct.  
- Choix du format de sortie.
