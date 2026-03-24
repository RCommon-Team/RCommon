import { readFileSync, writeFileSync } from 'fs';
import { JSONSchema7Definition } from '../entities/types';
import { getSchemaMarkdown } from './walker';
import { parentSchema } from '../entities/objects';
import jsYaml from 'js-yaml';

/**
 * Some descriptions have new-line characters which can cause rendering issues inside of md tables.
 * This helper removes the new-line character and provides a single block of text.
 */
export function removeNewLineCharacter(text: string) {
  return text.replace(/\n/g, ' ');
}

/**
 * To make the metadata structure flow a bit easier, we'll avoid the "duplication" of a metadata object followed by
 * `V1`. As an example, there is a `Command` object and a `CommandV1` object that acts as the definition of that object;
 * as all metadata objects need `kind` and `version` fields, this allows us to front-load the object's definition and skip
 * the "redundancy" of these two fields while also not duplicating content (description of the object).
 */
export function isV1Content(metadataObject: JSONSchema7Definition): boolean {
  return !!metadataObject.title?.includes('V1');
}

/**
 * When we write to a file, we need to find any content that appears after ## Metadata structure
 * ☝️ This string is used as our target and we wipe everything on the page after it and replace
 * it with the updated markdown.
 */
export function updatePageMarkdown(filePath: string, newMetadataMarkdown: string): boolean {
  try {
    const existingContents = readFileSync(filePath, 'utf-8');

    const parts = existingContents.split('## Metadata structure');
    let newContents = parts[0] + '## Metadata structure\n\n' + newMetadataMarkdown;

    writeFileSync(filePath, newContents, 'utf-8');

    console.log(`    ✅ markdown updated for ${filePath}`);
    return true;
  } catch (error) {
    console.error(`Failed to update Markdown: ${error}`);
    return false;
  }
}

export function generatePageMarkdown(fileName: string, metadataObjectTitles: string[]) {
  let pageMarkdown = '';

  metadataObjectTitles.map(metadataObjectTitle => {
    // Loop through each schema in case there are multiple
    const metadataObjectSchema = parentSchema
      .flatMap(schema => findSchemaDefinitionByTitle(schema, metadataObjectTitle)) // search in each schema
      .find(result => !!result); // stop as soon as a match is found

    if (metadataObjectSchema) {
      pageMarkdown += getSchemaMarkdown(metadataObjectSchema);
    } else {
      console.warn('Schema not found for: ', metadataObjectTitle);
    }
  });

  updatePageMarkdown(`../../docs/reference/metadata-reference/${fileName}.mdx`, pageMarkdown);
}

export function generateSchemaObjectMarkdown(
  metadataObject: JSONSchema7Definition,
  markdownValue: string,
  rootTitle: string,
  isSource: boolean = false
): string {
  let markdown = '';

  const title = getTitle(metadataObject);
  markdown += `\n${isSource ? '###' : '####'} ${title} {${getRefAnchor(metadataObject, rootTitle)}}\n\n`;

  if (metadataObject.description) markdown += `${metadataObject.description}\n\n`;

  markdown += markdownValue;

  if (metadataObject.examples) {
    markdown += getExamples(metadataObject);
  }

  return markdown;
}

export function generateScalarMarkdown(
  metadataObject: JSONSchema7Definition,
  scalarValue: string,
  rootTitle: string,
  isSource: boolean = false
): string {
  const markdownValue = `\n**Value:** ${scalarValue}`;
  return generateSchemaObjectMarkdown(metadataObject, markdownValue, rootTitle, isSource);
}

/**
 * This function allows us to identify and isolate a particular metadata object based on its title.
 */
export function findSchemaDefinitionByTitle(schema: JSONSchema7Definition, objectTitle: string): JSONSchema7Definition {
  if (!schema) {
    return;
  }

  schema = simplifyMetadataDefinition(schema);
  if (schema.$ref) {
    schema = handleRef(schema);
  }

  if (schema.title === objectTitle) {
    return schema;
  }

  let potentialSchemas: JSONSchema7Definition[] = [];
  if (!schema.type) {
    potentialSchemas = [...(schema.allOf || []), ...(schema.oneOf || []), ...(schema.anyOf || [])];
  } else if (schema.type === 'object') {
    // TODO: fix infinite loop
    // potentialSchemas = schema.properties ? Object.values(schema.properties) : [schema.additionalProperties];
  } else if (schema.type === 'array') {
    potentialSchemas = [getArrayItemType(schema)];
  }

  for (let potentialSchema of potentialSchemas) {
    const foundSchema = findSchemaDefinitionByTitle(potentialSchema, objectTitle);
    if (foundSchema) {
      return foundSchema;
    }
  }
}

export function getType(metadataObject: JSONSchema7Definition): string | void {
  if (metadataObject.type) {
    if (Array.isArray(metadataObject.type)) {
      return metadataObject.type[0];
    } else {
      return metadataObject.type;
    }
  }
}

export function getArrayItemType(metadataObject: JSONSchema7Definition): JSONSchema7Definition {
  return Array.isArray(metadataObject.items) ? metadataObject.items[0] : metadataObject.items;
}

export function getTitle(metadataObject: JSONSchema7Definition): string {
  if (metadataObject.title) {
    return metadataObject.title;
  }

  if (metadataObject.$ref) {
    return getParsedRef(metadataObject.$ref);
  }

  // Handle wrapper objects with kind and version (like Model oneOf entries)
  if (metadataObject.properties?.kind?.enum?.[0] && metadataObject.properties?.version?.enum?.[0]) {
    const kind = metadataObject.properties.kind.enum[0];
    const version = metadataObject.properties.version.enum[0];
    return `${kind} (${version})`; // e.g., "Model (v1)"
  }

  return undefined;
}

export function getDescription(metadataObject: JSONSchema7Definition): string {
  return metadataObject.description ? removeNewLineCharacter(metadataObject.description) : '';
}

/**
 * Entities whose v2 examples should be excluded from documentation.
 */
const EXCLUDED_V2_EXAMPLE_ENTITIES = [
  'TypePermissions',
  'ModelPermissions',
  'CommandPermissions'
];

/**
 * Check if an example should be excluded based on version.
 * Filters out v2 examples for specific permission types.
 */
function shouldExcludeExample(title: string | undefined, example: any): boolean {
  if (!title || !example) return false;

  // Check if this is a permission entity that should exclude v2 examples
  if (EXCLUDED_V2_EXAMPLE_ENTITIES.includes(title)) {
    // Check if the example has version: "v2"
    if (example.version === 'v2') {
      return true;
    }
  }

  return false;
}

export function getExamples(metadataObject: JSONSchema7Definition): string {
  let examples = '';
  if (metadataObject.examples) {
    const title = getTitle(metadataObject);

    // Filter out excluded examples
    const filteredExamples = metadataObject.examples.filter(
      example => !shouldExcludeExample(title, example)
    );

    // Only generate examples section if there are examples left after filtering
    if (filteredExamples.length > 0) {
      examples =
        `\n **Example${filteredExamples.length > 1 ? 's' : ''}:**` +
        filteredExamples.map(example => `\n\n\`\`\`yaml\n${jsYaml.dump(example)}\`\`\``).join('\n\n');
    }
  }

  return examples;
}

// For formatting heading tags
export function formatLink(linkText: string): string {
  if (linkText) {
    return linkText.toLowerCase().replace(' ', '-');
  }
}

export function getRefAnchor(metadataObject: JSONSchema7Definition, rootTitle: string): string {
  return `#${formatLink(`${rootTitle}-${getTitle(metadataObject)}`)}`;
}

export function getRefLink(metadataObject: JSONSchema7Definition, rootTitle: string): string {
  return `[${getTitle(metadataObject)}](${getRefAnchor(metadataObject, rootTitle)})`;
}

export function getParsedRef(ref: string): string {
  return ref?.split('/')?.pop();
}

export function handleRef(metadataObject: JSONSchema7Definition): JSONSchema7Definition {
  const { $ref, ...strippedSchema } = metadataObject;

  const refPath = $ref.split('/');

  let refObject: JSONSchema7Definition | undefined;

  // Iterate through each schema in the array to resolve the reference
  for (const schema of parentSchema) {
    let currentObject: any = schema;

    // Then, once in each, navigate thoes ref paths
    refPath.forEach(path => {
      if (path !== '#') {
        currentObject = currentObject?.[path];
      }
    });

    // If the reference was resolved, assign the result to refObject and break
    if (currentObject !== undefined) {
      // We found the right schema 🎉
      refObject = currentObject;
      break;
    }
  }

  if (refObject !== undefined) {
    refObject = simplifyMetadataDefinition({ ...strippedSchema, ...(refObject as JSONSchema7Definition) });

    if (refObject && refObject.$ref) {
      refObject = handleRef(refObject);
    }

    return refObject;
  } else {
    console.warn('Ref not found: ', $ref);
    return metadataObject;
  }
}

export function simplifyMetadataDefinition(metadataObject: JSONSchema7Definition): JSONSchema7Definition {
  let simplifiedSchema = metadataObject;
  if (metadataObject?.allOf?.length === 1) {
    const { allOf, ...strippedSchema } = metadataObject;
    simplifiedSchema = {
      ...strippedSchema,
      ...simplifyMetadataDefinition(allOf[0]),
    };
  } else if (metadataObject?.oneOf?.length === 1) {
    const { oneOf, ...strippedSchema } = metadataObject;
    simplifiedSchema = {
      ...strippedSchema,
      ...simplifyMetadataDefinition(oneOf[0]),
    };
  } else if (metadataObject?.anyOf?.length === 1) {
    const { anyOf, ...strippedSchema } = metadataObject;
    simplifiedSchema = {
      ...strippedSchema,
      ...simplifyMetadataDefinition(anyOf[0]),
    };
  }

  return simplifiedSchema;
}

export function isObjectType(type: string): boolean {
  return type === 'object';
}

export function isArrayType(type: string): boolean {
  return type === 'array';
}

export function isScalarType(type: string): boolean {
  const scalarTypes = [`string`, `number`, `integer`, `null`, `boolean`];

  return scalarTypes.includes(type);
}

export function isNullType(type: string): boolean {
  return type === 'null';
}

// Checks if the given metadataObject is a `oneOf` with each variant being discriminated
// by the presence of a particular field and the variant specific fields nested
// within.
export function isExternallyTaggedOneOf(metadataObject: JSONSchema7Definition): boolean {
  if (metadataObject.oneOf) {
    return (
      metadataObject.oneOf.length > 1 &&
      metadataObject.oneOf.every(sub_object => {
        let result =
          sub_object.properties &&
          Object.keys(sub_object.properties).length === 1 &&
          sub_object.required &&
          Object.keys(sub_object.required).length === 1;
        return result;
      })
    );
  } else {
    return false;
  }
}

// Checks if the given object is `anyOf` either null or a metadataObject
export function isExternallyTaggedNullable(metadataObject: JSONSchema7Definition): boolean {
  return (
    metadataObject.anyOf &&
    metadataObject.anyOf.length === 2 &&
    metadataObject.anyOf.some(sub_object => !Array.isArray(sub_object.type) && isNullType(sub_object.type))
  );
}
