<template>
	<v-row justify="space-between" class="flex-nowrap" no-gutters>
		<v-col cols="9">
			<v-subheader v-if="!editMode" class="form-label text-no-wrap">{{ newValue }}</v-subheader>
			<p-text-field v-else v-model="newValue" />
		</v-col>
		<v-col cols="3" justify="right">
			<p-btn v-if="!editMode" :button-type="editBtn" :disabled="disabled" :height="50" @click="edit" />
			<p-btn v-else :button-type="saveBtn" :disabled="disabled" :height="50" @click="save" />
		</v-col>
	</v-row>
</template>

<script lang="ts">
import { Component, Vue, Prop } from 'vue-property-decorator';
import ButtonType from '@enums/buttonType';

@Component
export default class EditableText extends Vue {
	@Prop({ required: false, type: String })
	readonly value!: string;

	@Prop({ required: false, type: Boolean })
	readonly disabled!: boolean;

	editBtn: ButtonType = ButtonType.Edit;
	saveBtn: ButtonType = ButtonType.Save;
	editMode: boolean = false;
	newValue: string = this.value;

	edit(): void {
		this.editMode = true;
	}

	save(): void {
		this.editMode = false;
		this.$emit('save', this.newValue);
	}
}
</script>
