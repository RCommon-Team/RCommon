import { walkSchemaToMarkdown } from '../src/logic';

describe('walkSchemaToMarkdown', () => {
  it('should generate markdown for a metadata object', () => {
    const metadata = {
      $id: 'https://hasura.io/jsonschemas/metadata/Model',
      title: 'Model',
      description:
        'The definition of a data model. A data model is a collection of objects of a particular type. Models can support one or more CRUD operations.',
      oneOf: [
        {
          type: 'object',
          required: ['definition', 'kind', 'version'],
          properties: {
            kind: {
              type: 'string',
              enum: ['Model'],
            },
            version: {
              type: 'string',
              enum: ['v1'],
            },
            definition: {
              $ref: '#/definitions/ModelV1',
            },
          },
          additionalProperties: false,
        },
      ],
    };

    const markdown = walkSchemaToMarkdown(metadata);

    expect(markdown).toContain('### Model');
    expect(markdown).toContain(
      'The definition of a data model. A data model is a collection of objects of a particular type. Models can support one or more CRUD operations.'
    );
    expect(markdown).toContain('| Name | Type | Required | Description |');
    expect(markdown).toContain('| `kind` | string | true |  |');
    expect(markdown).toContain('| `version` | string | true |  |');
    expect(markdown).toContain('| `definition` | [ModelV1](#ModelV1) | true |  |');
  });
});
